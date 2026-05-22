using System;
using System.Collections.Generic;

namespace WindCore.Core.Protocol;

/// <summary>
/// 控制命令载荷
/// </summary>
public class ControlPayload
{
    /// <summary>
    /// 子系统标识
    /// </summary>
    public string Subsystem { get; set; } = "";

    /// <summary>
    /// 动作（start/stop/set/emergency_stop）
    /// </summary>
    public string Action { get; set; } = "";

    /// <summary>
    /// 参数键值对
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 数据载荷
/// </summary>
public class DataPayload
{
    /// <summary>
    /// 数据来源标识
    /// </summary>
    public string Source { get; set; } = "";

    /// <summary>
    /// 数据时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 通道数据列表
    /// </summary>
    public List<ChannelData> Channels { get; set; } = new();
}

/// <summary>
/// 单通道数据
/// </summary>
public class ChannelData
{
    /// <summary>
    /// 通道ID
    /// </summary>
    public string ChannelId { get; set; } = "";

    /// <summary>
    /// 通道名称
    /// </summary>
    public string ChannelName { get; set; } = "";

    /// <summary>
    /// 原始值
    /// </summary>
    public double RawValue { get; set; }

    /// <summary>
    /// 工程量转换值
    /// </summary>
    public double EngineeringValue { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = "";

    /// <summary>
    /// 是否异常
    /// </summary>
    public bool IsAlarm { get; set; }
}

/// <summary>
/// 报警事件载荷
/// </summary>
public class AlarmPayload
{
    /// <summary>
    /// 报警等级（1=急停, 2=警示, 3=提示）
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 所属系统
    /// </summary>
    public string System { get; set; } = "";

    /// <summary>
    /// 报警点描述
    /// </summary>
    public string Point { get; set; } = "";

    /// <summary>
    /// 当前值
    /// </summary>
    public double CurrentValue { get; set; }

    /// <summary>
    /// 阈值
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// 事件类型（triggered/cleared）
    /// </summary>
    public string EventType { get; set; } = "triggered";

    /// <summary>
    /// 事件时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 存储指令载荷
/// </summary>
public class StorePayload
{
    /// <summary>
    /// 试验ID
    /// </summary>
    public string ExperimentId { get; set; } = "";

    /// <summary>
    /// 存储类型（csv/database）
    /// </summary>
    public string StoreType { get; set; } = "";

    /// <summary>
    /// 数据内容（JSON）
    /// </summary>
    public string Data { get; set; } = "";

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 试验管理指令载荷
/// </summary>
public class ExperimentPayload
{
    /// <summary>
    /// 试验ID
    /// </summary>
    public string ExperimentId { get; set; } = "";

    /// <summary>
    /// 试验名称
    /// </summary>
    public string ExperimentName { get; set; } = "";

    /// <summary>
    /// 动作（create/start/pause/stop/complete）
    /// </summary>
    public string Action { get; set; } = "";

    /// <summary>
    /// 扩展参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 配置同步载荷
/// </summary>
public class ConfigPayload
{
    /// <summary>
    /// 配置键
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// 配置值（JSON）
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; }
}
