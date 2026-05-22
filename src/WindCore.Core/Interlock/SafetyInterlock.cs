using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindCore.Core.Interlock;

/// <summary>
/// 安全联锁线程 - 独立线程 100ms 周期检测
/// 变量超限自动触发告警/停车联动，覆盖 22 条安全联锁事件
/// </summary>
public class SafetyInterlock : IDisposable
{
    private readonly Timer _detectionTimer;
    private readonly List<SafetyEvent> _events;
    private readonly Dictionary<string, Func<double>> _monitors;
    private readonly Action<AlarmRecord> _onAlarmTriggered;
    private readonly Action<string>? _logger;
    private readonly object _lock = new();

    public SafetyInterlock(Action<AlarmRecord> onAlarmTriggered, Action<string>? logger = null)
    {
        _onAlarmTriggered = onAlarmTriggered ?? throw new ArgumentNullException(nameof(onAlarmTriggered));
        _logger = logger;
        _events = new List<SafetyEvent>(SafetyEventDefinitions.All);
        _monitors = new Dictionary<string, Func<double>>();
        _detectionTimer = new Timer(DetectionCycle, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 注册监控变量
    /// </summary>
    /// <param name="key">变量标识</param>
    /// <param name="getter">获取当前值的函数</param>
    public void RegisterMonitor(string key, Func<double> getter)
    {
        lock (_lock)
        {
            _monitors[key] = getter;
        }
    }

    /// <summary>
    /// 注销监控变量
    /// </summary>
    public void UnregisterMonitor(string key)
    {
        lock (_lock)
        {
            _monitors.Remove(key);
        }
    }

    /// <summary>
    /// 启动安全联锁检测（100ms 周期）
    /// </summary>
    public void Start()
    {
        _detectionTimer.Change(0, 100);
        _logger?.Invoke("[SafetyInterlock] Started, detection cycle: 100ms");
    }

    /// <summary>
    /// 停止安全联锁检测
    /// </summary>
    public void Stop()
    {
        _detectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _logger?.Invoke("[SafetyInterlock] Stopped");
    }

    private void DetectionCycle(object? state)
    {
        try
        {
            var monitorsSnapshot = new Dictionary<string, Func<double>>(_monitors);

            foreach (var evt in _events)
            {
                if (!evt.Enabled) continue;
                if (!monitorsSnapshot.TryGetValue(evt.MonitorKey, out var getter)) continue;

                double currentValue = getter();

                bool triggered = evt.CheckThreshold(currentValue);
                bool wasTriggered = evt.LastTriggered;

                if (triggered && !wasTriggered)
                {
                    // 新触发报警
                    evt.LastTriggered = true;
                    evt.TriggeredAt = DateTime.Now;

                    var record = new AlarmRecord
                    {
                        Level = evt.Level,
                        System = evt.SystemName,
                        Point = evt.PointName,
                        CurrentValue = currentValue,
                        Threshold = evt.Threshold,
                        EventType = "triggered",
                        Timestamp = DateTime.Now,
                        Action = evt.ActionDescription,
                    };

                    _logger?.Invoke($"[SafetyInterlock] ALARM: {evt.SystemName} - {evt.PointName} = {currentValue} (threshold: {evt.Threshold})");
                    _onAlarmTriggered(record);
                }
                else if (!triggered && wasTriggered)
                {
                    // 报警消除
                    evt.LastTriggered = false;

                    var record = new AlarmRecord
                    {
                        Level = evt.Level,
                        System = evt.SystemName,
                        Point = evt.PointName,
                        CurrentValue = currentValue,
                        Threshold = evt.Threshold,
                        EventType = "cleared",
                        Timestamp = DateTime.Now,
                        Action = "",
                    };

                    _logger?.Invoke($"[SafetyInterlock] CLEARED: {evt.SystemName} - {evt.PointName}");
                    _onAlarmTriggered(record);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[SafetyInterlock] Detection error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _detectionTimer.Dispose();
    }
}

/// <summary>
/// 安全联锁事件定义
/// </summary>
public class SafetyEvent
{
    public string Id { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string PointName { get; set; } = "";
    public string MonitorKey { get; set; } = "";

    /// <summary>
    /// 报警等级：1=急停, 2=警示, 3=提示
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 阈值比较方式
    /// </summary>
    public ThresholdComparison Comparison { get; set; }

    /// <summary>
    /// 阈值
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// 系统响应动作描述
    /// </summary>
    public string ActionDescription { get; set; } = "";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 上次是否触发
    /// </summary>
    public bool LastTriggered { get; set; }

    /// <summary>
    /// 触发时间
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    public bool CheckThreshold(double currentValue)
    {
        return Comparison switch
        {
            ThresholdComparison.GreaterThan => currentValue > Threshold,
            ThresholdComparison.LessThan => currentValue < Threshold,
            ThresholdComparison.GreaterThanOrEqual => currentValue >= Threshold,
            ThresholdComparison.LessThanOrEqual => currentValue <= Threshold,
            _ => false,
        };
    }
}

/// <summary>
/// 阈值比较方式
/// </summary>
public enum ThresholdComparison
{
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
}

/// <summary>
/// 22 条安全联锁事件定义
/// </summary>
public static class SafetyEventDefinitions
{
    public static readonly List<SafetyEvent> All = new()
    {
        // 动力控制系统
        new SafetyEvent { Id = "S01", SystemName = "动力控制系统", PointName = "轴温", MonitorKey = "motor_sha_temp", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 80, ActionDescription = "主控界面警示，变频电机停车" },
        new SafetyEvent { Id = "S02", SystemName = "动力控制系统", PointName = "绕组温度", MonitorKey = "motor_winding_temp", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 120, ActionDescription = "主控界面警示，变频电机停车" },
        new SafetyEvent { Id = "S03", SystemName = "动力控制系统", PointName = "轴振", MonitorKey = "motor_sha_vibration", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 10, ActionDescription = "主控界面警示，变频电机停车" },
        new SafetyEvent { Id = "S04", SystemName = "动力控制系统", PointName = "变频器故障", MonitorKey = "inverter_fault", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 0.5, ActionDescription = "主控界面警示，变频电机停车" },
        new SafetyEvent { Id = "S05", SystemName = "动力控制系统", PointName = "通讯中断", MonitorKey = "motor_comms_status", Level = 1, Comparison = ThresholdComparison.LessThan, Threshold = 1, ActionDescription = "主控界面警告，蜂鸣器报警，变频电机急停" },
        new SafetyEvent { Id = "S06", SystemName = "动力控制系统", PointName = "急停按钮", MonitorKey = "motor_emergency_stop", Level = 1, Comparison = ThresholdComparison.GreaterThan, Threshold = 0.5, ActionDescription = "主控界面警告，蜂鸣器报警，变频电机急停" },

        // 冷却控制系统
        new SafetyEvent { Id = "S07", SystemName = "冷却控制系统", PointName = "水箱液位<10%", MonitorKey = "cooling_tank_level", Level = 2, Comparison = ThresholdComparison.LessThan, Threshold = 10, ActionDescription = "主控界面警示，供水泵降转速后停机" },
        new SafetyEvent { Id = "S08", SystemName = "冷却控制系统", PointName = "管路温度<5℃", MonitorKey = "cooling_pipe_temp", Level = 2, Comparison = ThresholdComparison.LessThan, Threshold = 5, ActionDescription = "主控界面警示，供水泵禁止启动" },
        new SafetyEvent { Id = "S09", SystemName = "冷却控制系统", PointName = "管路压力>1MPa", MonitorKey = "cooling_pipe_pressure", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 1.0, ActionDescription = "主控界面警示，供水泵降转速后停机" },
        new SafetyEvent { Id = "S10", SystemName = "冷却控制系统", PointName = "供水温度>80℃", MonitorKey = "cooling_water_temp", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 80, ActionDescription = "主控界面警示，加热水箱加热停止" },

        // 数据采集系统
        new SafetyEvent { Id = "S11", SystemName = "数据采集系统", PointName = "风速异常", MonitorKey = "wind_speed_anomaly", Level = 3, Comparison = ThresholdComparison.GreaterThan, Threshold = 130, ActionDescription = "主控界面提示，提醒操作人员检查" },
        new SafetyEvent { Id = "S12", SystemName = "数据采集系统", PointName = "压力异常", MonitorKey = "pressure_anomaly", Level = 3, Comparison = ThresholdComparison.GreaterThan, Threshold = 500, ActionDescription = "主控界面提示，提醒操作人员检查" },
        new SafetyEvent { Id = "S13", SystemName = "数据采集系统", PointName = "温湿度异常", MonitorKey = "temp_humidity_anomaly", Level = 3, Comparison = ThresholdComparison.GreaterThan, Threshold = 55, ActionDescription = "主控界面提示，提醒操作人员检查" },

        // 台架控制系统
        new SafetyEvent { Id = "S14", SystemName = "台架控制系统", PointName = "侧滑角限位", MonitorKey = "yaw_angle_limit", Level = 2, Comparison = ThresholdComparison.GreaterThanOrEqual, Threshold = 20, ActionDescription = "主控界面警示，伺服电机停车" },
        new SafetyEvent { Id = "S15", SystemName = "台架控制系统", PointName = "俯仰角限位", MonitorKey = "pitch_angle_limit", Level = 2, Comparison = ThresholdComparison.GreaterThanOrEqual, Threshold = 20, ActionDescription = "主控界面警示，伺服电机停车" },
        new SafetyEvent { Id = "S16", SystemName = "台架控制系统", PointName = "伺服控制器故障", MonitorKey = "servo_fault", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 0.5, ActionDescription = "主控界面警示，伺服电机停车" },
        new SafetyEvent { Id = "S17", SystemName = "台架控制系统", PointName = "通讯中断", MonitorKey = "servo_comms_status", Level = 1, Comparison = ThresholdComparison.LessThan, Threshold = 1, ActionDescription = "主控界面警告，蜂鸣器报警，伺服电机急停" },
        new SafetyEvent { Id = "S18", SystemName = "台架控制系统", PointName = "急停按钮", MonitorKey = "servo_emergency_stop", Level = 1, Comparison = ThresholdComparison.GreaterThan, Threshold = 0.5, ActionDescription = "主控界面警告，蜂鸣器报警，伺服电机急停" },

        // 负载系统
        new SafetyEvent { Id = "S19", SystemName = "负载系统", PointName = "系统故障", MonitorKey = "rat_fault", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 0.5, ActionDescription = "主控界面警示，负载系统、动力控制系统停车" },

        // 桨距角测量系统
        new SafetyEvent { Id = "S20", SystemName = "桨距角测量系统", PointName = "系统故障", MonitorKey = "blade_pitch_fault", Level = 2, Comparison = ThresholdComparison.GreaterThan, Threshold = 0.5, ActionDescription = "主控界面警示，桨距角测量系统、动力控制系统停车" },

        // 驻室门
        new SafetyEvent { Id = "S21", SystemName = "驻室门", PointName = "大门未关", MonitorKey = "door_closed", Level = 2, Comparison = ThresholdComparison.LessThan, Threshold = 0.5, ActionDescription = "主控界面警示，禁止起风" },

        // 预留点位
        new SafetyEvent { Id = "S22", SystemName = "其他", PointName = "预留点位", MonitorKey = "reserved", Level = 3, Comparison = ThresholdComparison.GreaterThan, Threshold = double.MaxValue, ActionDescription = "根据业主需求及现场实际增补", Enabled = false },
    };
}

/// <summary>
/// 报警记录
/// </summary>
public class AlarmRecord
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
    public string EventType { get; set; } = "";

    /// <summary>
    /// 事件时间
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 联动动作描述
    /// </summary>
    public string Action { get; set; } = "";
}
