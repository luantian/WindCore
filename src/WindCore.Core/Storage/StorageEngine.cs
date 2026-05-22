using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WindCore.Core.Storage;

/// <summary>
/// 队列存储引擎 - 基于队列的异步数据存储机制
/// 避免数据冲突或处理速度不足导致丢失
/// </summary>
public class StorageEngine : IDisposable
{
    private readonly BlockingCollection<StoreItem> _queue = new(new ConcurrentQueue<StoreItem>());
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly StorageConfig _config;
    private readonly Action<string>? _logger;
    private readonly object _csvLock = new();

    /// <summary>
    /// 当前队列长度
    /// </summary>
    public int QueueCount => _queue.Count;

    private long _processedCount;
    private long _failedCount;

    /// <summary>
    /// 已处理项目数
    /// </summary>
    public long ProcessedCount => _processedCount;

    /// <summary>
    /// 存储失败计数
    /// </summary>
    public long FailedCount => _failedCount;

    public StorageEngine(StorageConfig config, Action<string>? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;

        _processingTask = Task.Run(() => ProcessLoop(_cts.Token), _cts.Token);
        _logger?.Invoke("[StorageEngine] Started");
    }

    /// <summary>
    /// 入队一条数据（异步，不阻塞调用方）
    /// </summary>
    public void Enqueue(StoreItem item)
    {
        if (_cts.IsCancellationRequested) return;
        _queue.Add(item, _cts.Token);
    }

    /// <summary>
    /// 批量入队
    /// </summary>
    public void EnqueueBatch(StoreItem[] items)
    {
        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _queue.CompleteAdding();
        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // Ignore
        }
        _cts.Dispose();
        _queue.Dispose();
        _logger?.Invoke("[StorageEngine] Disposed");
    }

    private void ProcessLoop(CancellationToken ct)
    {
        _logger?.Invoke("[StorageEngine] Processing loop started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_queue.TryTake(out var item, 100, ct))
                {
                    ProcessItem(item);
                    Interlocked.Increment(ref _processedCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedCount);
                _logger?.Invoke($"[StorageEngine] Error processing item: {ex.Message}");
            }
        }

        _logger?.Invoke("[StorageEngine] Processing loop stopped");
    }

    private void ProcessItem(StoreItem item)
    {
        switch (item.StoreType)
        {
            case StoreType.Csv:
                WriteToCsv(item);
                break;

            case StoreType.Database:
                WriteToDatabase(item);
                break;

            default:
                _logger?.Invoke($"[StorageEngine] Unknown store type: {item.StoreType}");
                break;
        }
    }

    private void WriteToCsv(StoreItem item)
    {
        string filePath = GetCsvFilePath(item);
        string directory = Path.GetDirectoryName(filePath) ?? "";

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        lock (_csvLock)
        {
            bool fileExists = File.Exists(filePath);

            using var writer = new StreamWriter(filePath, append: true);

            if (!fileExists)
            {
                // 写表头
                writer.WriteLine("Timestamp,ChannelId,ChannelName,RawValue,EngineeringValue,Unit");
            }

            foreach (var channel in item.Channels)
            {
                writer.WriteLine($"{item.Timestamp:O},{channel.ChannelId},{channel.ChannelName},{channel.RawValue},{channel.EngineeringValue},{channel.Unit}");
            }
        }
    }

    private string GetCsvFilePath(StoreItem item)
    {
        string baseDir = _config.CsvBasePath;
        string experimentDir = string.IsNullOrEmpty(item.ExperimentId)
            ? "default"
            : item.ExperimentId;

        string directory = Path.Combine(baseDir, experimentDir);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string fileName = $"{item.Timestamp:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(directory, fileName);
    }

    private void WriteToDatabase(StoreItem item)
    {
        try
        {
            using var writer = new DatabaseWriter("192.168.1.50", 3306, "windcore_db", "root", "");
            if (writer.Connect())
            {
                writer.WriteBatch(new[] { item });
            }
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[StorageEngine] Database write error: {ex.Message}");
        }
    }
}

/// <summary>
/// 存储项
/// </summary>
public class StoreItem
{
    /// <summary>
    /// 试验ID
    /// </summary>
    public string ExperimentId { get; set; } = "";

    /// <summary>
    /// 存储类型
    /// </summary>
    public StoreType StoreType { get; set; }

    /// <summary>
    /// 数据时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 通道数据列表
    /// </summary>
    public List<ChannelDataItem> Channels { get; set; } = new();
}

/// <summary>
/// 通道数据项
/// </summary>
public class ChannelDataItem
{
    public string ChannelId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public double RawValue { get; set; }
    public double EngineeringValue { get; set; }
    public string Unit { get; set; } = "";
}

/// <summary>
/// 存储类型
/// </summary>
public enum StoreType
{
    Csv,
    Database,
}

/// <summary>
/// 存储引擎配置
/// </summary>
public class StorageConfig
{
    /// <summary>
    /// CSV 文件存储根路径
    /// </summary>
    public string CsvBasePath { get; set; } = Path.Combine(Environment.CurrentDirectory, "data", "csv");
}
