using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace WindCore.Core.EventQueue;

/// <summary>
/// 事件队列 - 基于 C# Event 封装带缓存触发机制的队列
/// 常驻异步/自销毁触发线程，CPU 占用率 < 30%
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
public class EventQueue<T> : IDisposable
{
    private readonly BlockingCollection<QueueItem<T>> _queue = new(new ConcurrentQueue<QueueItem<T>>());
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly Func<T, Task> _handler;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;
    private readonly string _queueName;
    private readonly Action<string>? _logger;

    /// <summary>
    /// 当前队列长度
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// 队列是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 事件触发前回调（可用于埋点统计）
    /// </summary>
    public event EventHandler<QueueItem<T>>? ItemEnqueued;

    /// <summary>
    /// 事件处理后回调
    /// </summary>
    public event EventHandler<QueueItem<T>>? ItemProcessed;

    /// <summary>
    /// 处理异常回调
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    public EventQueue(
        Func<T, Task> handler,
        string queueName = "default",
        int batchSize = 1,
        TimeSpan? pollInterval = null,
        Action<string>? logger = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _queueName = queueName;
        _batchSize = Math.Max(1, batchSize);
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(10);
        _logger = logger;

        _processingTask = Task.Run(() => ProcessLoop(_cts.Token), _cts.Token);
        IsRunning = true;
    }

    /// <summary>
    /// 入队一个事件
    /// </summary>
    public void Enqueue(T data)
    {
        var item = new QueueItem<T> { Data = data, EnqueuedAt = DateTime.UtcNow };
        _queue.Add(item, _cts.Token);
        ItemEnqueued?.Invoke(this, item);
    }

    /// <summary>
    /// 停止队列（等待剩余项目处理完毕）
    /// </summary>
    public void Stop()
    {
        _queue.CompleteAdding();
        IsRunning = false;
    }

    /// <summary>
    /// 立即停止队列（丢弃未处理项目）
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        _queue.CompleteAdding();
        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore cancellation/aggregate exceptions during disposal
        }
        _cts.Dispose();
        _queue.Dispose();
    }

    private async Task ProcessLoop(CancellationToken ct)
    {
        _logger?.Invoke($"[{_queueName}] Event queue processing started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var batch = new QueueItem<T>[_batchSize];
                int count = 0;

                // 取第一批
                if (_queue.TryTake(out var first, Timeout.Infinite, ct))
                {
                    batch[0] = first;
                    count = 1;
                }

                // 批量取（非阻塞）
                for (int i = 1; i < _batchSize; i++)
                {
                    if (_queue.TryTake(out var item, 0))
                    {
                        batch[i] = item;
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            await _handler(batch[i].Data).ConfigureAwait(false);
                            batch[i].ProcessedAt = DateTime.UtcNow;
                            ItemProcessed?.Invoke(this, batch[i]);
                        }
                        catch (Exception ex)
                        {
                            ErrorOccurred?.Invoke(this, ex);
                            _logger?.Invoke($"[{_queueName}] Error processing item: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // 队列为空，短暂休眠避免 CPU 空转
                    await Task.Delay(_pollInterval, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                _logger?.Invoke($"[{_queueName}] Unexpected error in processing loop: {ex.Message}");
            }
        }

        _logger?.Invoke($"[{_queueName}] Event queue processing stopped");
    }
}

/// <summary>
/// 队列项（携带元数据）
/// </summary>
public class QueueItem<T>
{
    public T Data { get; set; } = default!;
    public DateTime EnqueuedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// 处理耗时（毫秒）
    /// </summary>
    public double? ProcessingMs => ProcessedAt?.Subtract(EnqueuedAt).TotalMilliseconds;
}

/// <summary>
/// 自销毁事件队列 - 处理完成后自动释放
/// </summary>
public class SelfDestroyingQueue<T> : IDisposable
{
    private readonly EventQueue<T> _queue;
    private readonly int _maxItems;
    private int _processedCount;
    private readonly Action? _onComplete;

    public SelfDestroyingQueue(
        Func<T, Task> handler,
        int maxItems,
        Action? onComplete = null,
        string queueName = "self-destroy",
        Action<string>? logger = null)
    {
        _maxItems = maxItems;
        _onComplete = onComplete;
        _queue = new EventQueue<T>(
            async data =>
            {
                await handler(data).ConfigureAwait(false);
                if (Interlocked.Increment(ref _processedCount) >= _maxItems)
                {
                    _queue!.Stop();
                    _onComplete?.Invoke();
                }
            },
            queueName: queueName,
            logger: logger);
    }

    public void Enqueue(T data) => _queue.Enqueue(data);

    public void Dispose() => _queue.Dispose();
}
