using System;
using System.Text.Json;

namespace WindCore.Core.Protocol;

/// <summary>
/// 统一通讯协议 - Command.Class
/// 主控/数采/数据库三套软件共用，统一命令集/数据解析/心跳命令
/// </summary>

/// <summary>
/// 命令类型枚举
/// </summary>
public enum CommandType
{
    /// <summary>
    /// 心跳包
    /// </summary>
    Heartbeat = 0,

    /// <summary>
    /// 控制命令（启停、参数设置等）
    /// </summary>
    Control = 1,

    /// <summary>
    /// 数据请求
    /// </summary>
    DataRequest = 2,

    /// <summary>
    /// 数据响应
    /// </summary>
    DataResponse = 3,

    /// <summary>
    /// 报警事件
    /// </summary>
    AlarmEvent = 4,

    /// <summary>
    /// 存储指令
    /// </summary>
    StoreCommand = 5,

    /// <summary>
    /// 试验管理指令
    /// </summary>
    ExperimentCommand = 6,

    /// <summary>
    /// 配置同步
    /// </summary>
    ConfigSync = 7,
}

/// <summary>
/// 响应状态码
/// </summary>
public enum ResponseCode
{
    /// <summary>
    /// 成功
    /// </summary>
    Ok = 0,

    /// <summary>
    /// 参数错误
    /// </summary>
    BadRequest = 400,

    /// <summary>
    /// 认证失败
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// 命令不支持
    /// </summary>
    NotImplemented = 501,

    /// <summary>
    /// 内部错误
    /// </summary>
    InternalError = 500,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout = 504,
}

/// <summary>
/// 统一命令请求
/// </summary>
public class CommandRequest
{
    /// <summary>
    /// 请求唯一ID（GUID）
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 命令类型
    /// </summary>
    public CommandType CommandType { get; set; }

    /// <summary>
    /// 源端标识（发送方）
    /// </summary>
    public string Source { get; set; } = "";

    /// <summary>
    /// 目标端标识（接收方）
    /// </summary>
    public string Target { get; set; } = "";

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 命令参数（JSON 字符串，由子类或调用方填充）
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// 序列化请求为 JSON 字节数组
    /// </summary>
    public byte[] ToBytes()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions.Default);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// 从字节数组反序列化请求
    /// </summary>
    public static CommandRequest FromBytes(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        var request = JsonSerializer.Deserialize<CommandRequest>(json, JsonOptions.Default)
            ?? throw new ProtocolException("Failed to deserialize CommandRequest");
        return request;
    }

    /// <summary>
    /// 从 JSON 字符串反序列化
    /// </summary>
    public static CommandRequest FromJson(string json)
    {
        var request = JsonSerializer.Deserialize<CommandRequest>(json, JsonOptions.Default)
            ?? throw new ProtocolException("Failed to deserialize CommandRequest from JSON");
        return request;
    }

    /// <summary>
    /// 获取 Payload 中指定类型的对象
    /// </summary>
    public T? GetPayload<T>() where T : class
    {
        if (string.IsNullOrEmpty(Payload)) return null;
        return JsonSerializer.Deserialize<T>(Payload, JsonOptions.Default);
    }

    /// <summary>
    /// 设置 Payload
    /// </summary>
    public CommandRequest WithPayload<T>(T payload)
    {
        Payload = JsonSerializer.Serialize(payload, JsonOptions.Default);
        return this;
    }
}

/// <summary>
/// 统一命令响应
/// </summary>
public class CommandResponse
{
    /// <summary>
    /// 对应的请求ID
    /// </summary>
    public string RequestId { get; set; } = "";

    /// <summary>
    /// 响应状态码
    /// </summary>
    public ResponseCode Code { get; set; }

    /// <summary>
    /// 状态描述
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// 响应时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 响应数据
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 序列化响应为 JSON 字节数组
    /// </summary>
    public byte[] ToBytes()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions.Default);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// 从字节数组反序列化响应
    /// </summary>
    public static CommandResponse FromBytes(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<CommandResponse>(json, JsonOptions.Default)
            ?? throw new ProtocolException("Failed to deserialize CommandResponse");
    }

    /// <summary>
    /// 获取 Data 中指定类型的对象
    /// </summary>
    public T? GetData<T>() where T : class
    {
        if (string.IsNullOrEmpty(Data)) return null;
        return JsonSerializer.Deserialize<T>(Data, JsonOptions.Default);
    }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static CommandResponse Ok(string requestId, object? data = null, string message = "OK")
    {
        var response = new CommandResponse
        {
            RequestId = requestId,
            Code = ResponseCode.Ok,
            Message = message,
        };
        if (data != null)
        {
            response.Data = JsonSerializer.Serialize(data, JsonOptions.Default);
        }
        return response;
    }

    /// <summary>
    /// 创建错误响应
    /// </summary>
    public static CommandResponse Error(string requestId, ResponseCode code, string message)
    {
        return new CommandResponse
        {
            RequestId = requestId,
            Code = code,
            Message = message,
        };
    }
}

/// <summary>
/// 心跳包数据
/// </summary>
public class HeartbeatPayload
{
    /// <summary>
    /// 发送端标识
    /// </summary>
    public string NodeId { get; set; } = "";

    /// <summary>
    /// 当前时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 附加信息（如 CPU 使用率、内存等）
    /// </summary>
    public string? ExtraInfo { get; set; }
}

/// <summary>
/// 协议异常
/// </summary>
public class ProtocolException : Exception
{
    public ProtocolException(string message) : base(message) { }
    public ProtocolException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// JSON 序列化选项（统一配置）
/// </summary>
internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
