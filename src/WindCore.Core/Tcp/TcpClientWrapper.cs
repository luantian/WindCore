using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindCore.Core.Protocol;

namespace WindCore.Core.Tcp;

/// <summary>
/// TCP 客户端封装 - 用于主控与数采/数据库/RAT/桨距角系统通信
/// </summary>
public class TcpClientWrapper : IDisposable
{
    private readonly TcpClient _client = new();
    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;
    private NetworkStream? _stream;
    private bool _disposed;
    private readonly object _connectLock = new();
    private readonly Action<string>? _logger;

    public bool IsConnected => _client?.Connected == true && _stream?.CanWrite == true;
    public string RemoteEndPoint => _client?.Client.RemoteEndPoint?.ToString() ?? "";

    public TcpClientWrapper(string host, int port, int timeoutMs = 5000, Action<string>? logger = null)
    {
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        lock (_connectLock)
        {
            if (_client.Connected) return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeoutMs);

        try
        {
            await _client.ConnectAsync(_host, _port, timeoutCts.Token).ConfigureAwait(false);
            _stream = _client.GetStream();
            _logger?.Invoke($"[TCP] Connected to {_host}:{_port}");
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"TCP connection to {_host}:{_port} timed out after {_timeoutMs}ms");
        }
    }

    public async Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            await ConnectAsync(ct).ConfigureAwait(false);
        }

        var data = request.ToBytes();

        // 发送 4 字节长度头 + 数据体
        var lengthHeader = BitConverter.GetBytes(data.Length);
        await _stream!.WriteAsync(lengthHeader, 0, 4, ct).ConfigureAwait(false);
        await _stream!.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);

        // 接收 4 字节长度头 + 响应体
        var headerBuffer = new byte[4];
        int bytesRead = 0;
        while (bytesRead < 4)
        {
            int read = await _stream!.ReadAsync(headerBuffer, bytesRead, 4 - bytesRead, ct).ConfigureAwait(false);
            if (read == 0) throw new ConnectionResetException("Connection closed by remote host");
            bytesRead += read;
        }

        int bodyLength = BitConverter.ToInt32(headerBuffer, 0);
        var bodyBuffer = new byte[bodyLength];
        bytesRead = 0;
        while (bytesRead < bodyLength)
        {
            int read = await _stream!.ReadAsync(bodyBuffer, bytesRead, bodyLength - bytesRead, ct).ConfigureAwait(false);
            if (read == 0) throw new ConnectionResetException("Connection closed by remote host");
            bytesRead += read;
        }

        return CommandResponse.FromBytes(bodyBuffer);
    }

    public void Disconnect()
    {
        lock (_connectLock)
        {
            _stream?.Close();
            _client?.Close();
            _logger?.Invoke("[TCP] Disconnected");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
        _client.Dispose();
    }
}

/// <summary>
/// TCP 服务器封装 - 接收来自客户端的命令
/// </summary>
public class TcpServerWrapper : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _port;
    private readonly Func<CommandRequest, Task<CommandResponse>> _requestHandler;
    private readonly CancellationTokenSource _cts = new();
    private readonly Action<string>? _logger;

    public TcpServerWrapper(int port, Func<CommandRequest, Task<CommandResponse>> requestHandler, Action<string>? logger = null)
    {
        _port = port;
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        _logger = logger;
        _listener = new TcpListener(IPAddress.Any, _port);
    }

    public void Start()
    {
        _listener.Start();
        _logger?.Invoke($"[TCP Server] Listening on port {_port}");
        _ = Task.Run(() => AcceptLoop(_cts.Token), _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
        _logger?.Invoke("[TCP Server] Stopped");
    }

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[TCP Server] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var stream = client.GetStream();
        var buffer = new byte[4];

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 读长度头
                int bytesRead = 0;
                while (bytesRead < 4)
                {
                    int read = await stream.ReadAsync(buffer, bytesRead, 4 - bytesRead, ct).ConfigureAwait(false);
                    if (read == 0) return; // 客户端断开
                    bytesRead += read;
                }

                int bodyLength = BitConverter.ToInt32(buffer, 0);
                var bodyBuffer = new byte[bodyLength];
                bytesRead = 0;
                while (bytesRead < bodyLength)
                {
                    int read = await stream.ReadAsync(bodyBuffer, bytesRead, bodyLength - bytesRead, ct).ConfigureAwait(false);
                    if (read == 0) return;
                    bytesRead += read;
                }

                // 解析请求
                var request = CommandRequest.FromBytes(bodyBuffer);

                // 处理请求
                var response = await _requestHandler(request).ConfigureAwait(false);

                // 发送响应
                var responseData = response.ToBytes();
                var responseHeader = BitConverter.GetBytes(responseData.Length);
                await stream.WriteAsync(responseHeader, 0, 4, ct).ConfigureAwait(false);
                await stream.WriteAsync(responseData, 0, responseData.Length, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[TCP Server] Client handler error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
        _listener?.Stop();
    }
}

/// <summary>
/// 连接重置异常
/// </summary>
public class ConnectionResetException : Exception
{
    public ConnectionResetException(string message) : base(message) { }
}
