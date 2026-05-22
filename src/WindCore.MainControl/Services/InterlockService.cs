using System;
using Avalonia.Threading;
using WindCore.Core.Interlock;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Services;

/// <summary>
/// 联锁服务 - 将 Core.SafetyInterlock 接入 UI
/// 注册 DataService 属性到 22 个安全事件监控点，报警自动写入 DataService.Alarms
/// </summary>
public class InterlockService : IDisposable
{
    private readonly SafetyInterlock _interlock;
    private readonly DataService _dataService;
    private readonly SettingsPanelViewModel _settings;

    public InterlockService(DataService dataService, SettingsPanelViewModel settings)
    {
        _dataService = dataService;
        _settings = settings;
        _interlock = new SafetyInterlock(OnAlarmTriggered);

        // 动力系统
        _interlock.RegisterMonitor("motor_sha_temp", () => dataService.MotorTemp);
        _interlock.RegisterMonitor("motor_winding_temp", () => dataService.MotorWindingTemp);
        _interlock.RegisterMonitor("motor_sha_vibration", () => 0); // 暂无传感器
        _interlock.RegisterMonitor("inverter_fault", () => dataService.InverterStatus == "正常" ? 0 : 1);
        _interlock.RegisterMonitor("motor_comms_status", () => 1); // 1=正常
        _interlock.RegisterMonitor("motor_emergency_stop", () => 0);

        // 冷却系统
        _interlock.RegisterMonitor("cooling_tank_level", () => dataService.TankLevel);
        _interlock.RegisterMonitor("cooling_pipe_temp", () => dataService.CoolingOutTemp);
        _interlock.RegisterMonitor("cooling_pipe_pressure", () => dataService.CoolingPressure);
        _interlock.RegisterMonitor("cooling_water_temp", () => dataService.CoolingTemp);

        // 数据采集
        _interlock.RegisterMonitor("wind_speed_anomaly", () => dataService.WindSpeed);
        _interlock.RegisterMonitor("pressure_anomaly", () => dataService.CoolingPressure * 100);
        _interlock.RegisterMonitor("temp_humidity_anomaly", () => dataService.CoolingTemp);

        // 台架系统
        _interlock.RegisterMonitor("yaw_angle_limit", () => Math.Abs(dataService.YawActual));
        _interlock.RegisterMonitor("pitch_angle_limit", () => Math.Abs(dataService.PitchActual));
        _interlock.RegisterMonitor("servo_fault", () => 0);
        _interlock.RegisterMonitor("servo_comms_status", () => 1);
        _interlock.RegisterMonitor("servo_emergency_stop", () => 0);

        // 负载/桨距角/门
        _interlock.RegisterMonitor("rat_fault", () => 0);
        _interlock.RegisterMonitor("blade_pitch_fault", () => 0);
        _interlock.RegisterMonitor("door_closed", () => 1); // 1=已关

        // 更新阈值从 SettingsPanelViewModel
        UpdateThresholdsFromSettings();
    }

    public void Start() => _interlock.Start();

    public void Stop() => _interlock.Stop();

    /// <summary>
    /// 从 SettingsPanelViewModel 读取最新阈值并更新安全事件定义
    /// </summary>
    public void UpdateThresholdsFromSettings()
    {
        foreach (var evt in SafetyEventDefinitions.All)
        {
            evt.Threshold = evt.MonitorKey switch
            {
                "motor_sha_temp" => _settings.ShaftTempLimit,
                "motor_winding_temp" => _settings.WindingTempLimit,
                "cooling_tank_level" => _settings.TankLevelMin,
                "cooling_pipe_temp" => _settings.PipeTempMin,
                "cooling_pipe_pressure" => _settings.PipePressureMax,
                "cooling_water_temp" => _settings.CoolingTempMax,
                _ => evt.Threshold,
            };
        }
    }

    private void OnAlarmTriggered(AlarmRecord record)
    {
        string desc = $"{record.System} - {record.Point}";
        if (record.EventType == "triggered")
        {
            desc += $" = {record.CurrentValue:F1} (阈值: {record.Threshold:F1})";
        }
        else
        {
            desc += " 已恢复";
        }

        string status = record.EventType == "cleared" ? "已恢复" : "未处理";

        // Level 1 报警自动停车
        if (record.Level == 1 && record.EventType == "triggered")
        {
            _dataService.Stop();
            desc += " [紧急停车]";
        }

        Dispatcher.UIThread.Post(() =>
        {
            _dataService.Alarms.Insert(0, new AlarmRecordViewModel
            {
                Level = record.Level,
                Time = record.Timestamp.ToString("HH:mm:ss"),
                Description = desc,
                Status = status,
            });
            while (_dataService.Alarms.Count > 100)
                _dataService.Alarms.RemoveAt(_dataService.Alarms.Count - 1);
        });
    }

    public void Dispose()
    {
        Stop();
        _interlock.Dispose();
    }
}
