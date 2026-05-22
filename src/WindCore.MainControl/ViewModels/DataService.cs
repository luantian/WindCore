using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WindCore.Core.Interlock;
using WindCore.Core.PID;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindCore.MainControl.ViewModels;

/// <summary>
/// 共享数据服务 - 所有页面通过此服务获取实时数据
/// 模拟数据源，实际项目中由 Modbus/TCP 通讯填充
/// </summary>
public partial class DataService : ObservableObject
{
    private readonly Random _rand = new();
    private readonly System.Timers.Timer _updateTimer;

    // 动力系统
    [ObservableProperty] private double _windSpeed;
    [ObservableProperty] private double _motorRpm;
    [ObservableProperty] private double _motorTemp;
    [ObservableProperty] private double _motorFrequency;
    [ObservableProperty] private double _motorCurrent;
    [ObservableProperty] private string _motorStatus = "运行中";
    [ObservableProperty] private double _motorWindingTemp;
    [ObservableProperty] private string _inverterStatus = "正常";
    [ObservableProperty] private bool _motorRunning = true;

    // 冷却系统
    [ObservableProperty] private double _coolingTemp;
    [ObservableProperty] private double _coolingOutTemp;
    [ObservableProperty] private double _coolingReturnTemp;
    [ObservableProperty] private double _coolingFlow;
    [ObservableProperty] private double _coolingPressure;
    [ObservableProperty] private string _pumpStatus = "运行中";
    [ObservableProperty] private double _tankLevel;

    // 台架系统
    [ObservableProperty] private double _pitchSetpoint;
    [ObservableProperty] private double _pitchActual;
    [ObservableProperty] private double _yawSetpoint;
    [ObservableProperty] private double _yawActual;
    [ObservableProperty] private double _pitchDeviation;
    [ObservableProperty] private double _yawDeviation;

    // RAT 负载系统
    [ObservableProperty] private double _ratThrust;
    [ObservableProperty] private double _ratRpm;
    [ObservableProperty] private double _ratTorque;
    [ObservableProperty] private double _ratPower;
    [ObservableProperty] private string _ratMode = "恒推力";
    [ObservableProperty] private string _sensorStatus = "正常";

    // 桨距角
    [ObservableProperty] private double _bladePitch1 = 15.2;
    [ObservableProperty] private double _bladePitch2 = 15.5;
    [ObservableProperty] private double _bladePitch3 = 15.0;
    [ObservableProperty] private double _bladePitchAvg;

    // 龙门架
    [ObservableProperty] private double _gantryX;
    [ObservableProperty] private double _gantryY;
    [ObservableProperty] private double _gantryZ;
    [ObservableProperty] private double _gantrySpeedX;
    [ObservableProperty] private double _gantrySpeedY;
    [ObservableProperty] private double _gantrySpeedZ;

    // 报警
    public ObservableCollection<AlarmRecordViewModel> Alarms { get; } = new();

    // 子系统通讯状态
    [ObservableProperty] private string _motorCommStatus = "未连接";
    [ObservableProperty] private string _coolingCommStatus = "未连接";
    [ObservableProperty] private string _standCommStatus = "未连接";
    [ObservableProperty] private string _ratCommStatus = "未连接";
    [ObservableProperty] private string _gantryCommStatus = "未连接";
    [ObservableProperty] private string _bladePitchCommStatus = "未连接";

    // 系统级状态
    [ObservableProperty] private string _plcCommStatus = "未连接";
    [ObservableProperty] private string _daqCommStatus = "未连接";
    [ObservableProperty] private string _dbCommStatus = "未连接";
    [ObservableProperty] private string _interlockStatus = "正常";
    [ObservableProperty] private string _experimentStatus = "空闲";
    [ObservableProperty] private string _currentExperimentName = "";

    // 历史数据缓冲区（用于波形图，保留最近 5000 点 ≈ 500 秒）
    private const int MaxHistory = 5000;
    private readonly List<double> _windSpeedHistory = new(MaxHistory);
    private readonly List<double> _motorTempHistory = new(MaxHistory);
    private readonly List<double> _ratThrustHistory = new(MaxHistory);
    private readonly List<double> _ratRpmHistory = new(MaxHistory);
    private readonly List<double> _coolingOutTempHistory = new(MaxHistory);
    private readonly List<double> _coolingReturnTempHistory = new(MaxHistory);
    private readonly List<double> _coolingFlowHistory = new(MaxHistory);
    private readonly List<double> _bladePitch1History = new(MaxHistory);
    private readonly List<double> _pitchActualHistory = new(MaxHistory);
    private readonly List<double> _yawActualHistory = new(MaxHistory);

    public IReadOnlyList<double> WindSpeedHistory => _windSpeedHistory;
    public IReadOnlyList<double> MotorTempHistory => _motorTempHistory;
    public IReadOnlyList<double> RatThrustHistory => _ratThrustHistory;
    public IReadOnlyList<double> RatRpmHistory => _ratRpmHistory;
    public IReadOnlyList<double> CoolingOutTempHistory => _coolingOutTempHistory;
    public IReadOnlyList<double> CoolingReturnTempHistory => _coolingReturnTempHistory;
    public IReadOnlyList<double> CoolingFlowHistory => _coolingFlowHistory;
    public IReadOnlyList<double> BladePitch1History => _bladePitch1History;
    public IReadOnlyList<double> PitchActualHistory => _pitchActualHistory;
    public IReadOnlyList<double> YawActualHistory => _yawActualHistory;

    public DataService()
    {
        // 初始化模拟值
        _windSpeed = 85.6;
        _motorRpm = 1480;
        _motorTemp = 32.5;
        _motorFrequency = 49.3;
        _motorCurrent = 24.5;
        _motorWindingTemp = 65;
        _coolingTemp = 32.5;
        _coolingOutTemp = 18.5;
        _coolingReturnTemp = 24.2;
        _coolingFlow = 12.8;
        _coolingPressure = 0.8;
        _tankLevel = 75;
        _pitchSetpoint = 5.0;
        _pitchActual = 5.0;
        _yawSetpoint = 0.0;
        _yawActual = 0.0;
        _ratThrust = 1250;
        _ratRpm = 4500;
        _ratTorque = 45.2;
        _ratPower = 12.8;
        _bladePitch1 = 15.2;
        _bladePitch2 = 15.5;
        _bladePitch3 = 15.0;
        _bladePitchAvg = 15.23;
        _gantryX = 100;
        _gantryY = 50;
        _gantryZ = 200;

        // 预填充几条报警记录
        Alarms.Add(new AlarmRecordViewModel { Level = 2, Time = "11:28:30", Description = "绕组温度超限 (65°C)", Status = "未处理" });
        Alarms.Add(new AlarmRecordViewModel { Level = 2, Time = "11:25:12", Description = "管路压力偏高 (0.8MPa)", Status = "未处理" });
        Alarms.Add(new AlarmRecordViewModel { Level = 3, Time = "11:20:05", Description = "风速波动 (±2%) 提醒检查", Status = "已确认" });
        Alarms.Add(new AlarmRecordViewModel { Level = 1, Time = "11:15:42", Description = "冷却液流量恢复正常", Status = "已恢复" });

        // 100ms 更新周期模拟数据变化
        _updateTimer = new System.Timers.Timer(100);
        _updateTimer.Elapsed += (_, _) => UpdateSimulatedData();
    }

    public void Start()
    {
        _updateTimer.Start();
    }

    public void Stop()
    {
        _updateTimer.Stop();
    }

    private void UpdateSimulatedData()
    {
        // 动力系统模拟
        WindSpeed = Clamp(WindSpeed + (_rand.NextDouble() * 2 - 1), 20, 120);
        MotorRpm = Clamp(MotorRpm + (_rand.NextDouble() * 30 - 15), 0, 1800);
        MotorTemp = Clamp(MotorTemp + (_rand.NextDouble() * 0.6 - 0.3), 0, 60);
        MotorFrequency = Clamp(MotorFrequency + (_rand.NextDouble() * 0.2 - 0.1), 45, 55);
        MotorCurrent = Clamp(MotorCurrent + (_rand.NextDouble() * 1 - 0.5), 0, 50);

        // 冷却系统模拟
        CoolingOutTemp = Clamp(CoolingOutTemp + (_rand.NextDouble() * 0.4 - 0.2), 10, 30);
        CoolingReturnTemp = Clamp(CoolingReturnTemp + (_rand.NextDouble() * 0.4 - 0.2), 15, 35);
        CoolingFlow = Clamp(CoolingFlow + (_rand.NextDouble() * 0.5 - 0.25), 0, 20);
        CoolingPressure = Clamp(CoolingPressure + (_rand.NextDouble() * 0.05 - 0.025), 0.3, 1.5);

        // 台架模拟
        PitchActual = PitchSetpoint + (_rand.NextDouble() * 0.2 - 0.1);
        YawActual = YawSetpoint + (_rand.NextDouble() * 0.2 - 0.1);
        PitchDeviation = PitchSetpoint - PitchActual;
        YawDeviation = YawSetpoint - YawActual;

        // RAT 模拟
        RatThrust = Clamp(RatThrust + (_rand.NextDouble() * 40 - 20), 0, 5000);
        RatRpm = Clamp(RatRpm + (_rand.NextDouble() * 100 - 50), 0, 10000);
        RatTorque = Clamp(RatTorque + (_rand.NextDouble() * 2 - 1), 0, 200);
        RatPower = RatTorque * RatRpm / 9550;

        // 桨距角 - 3个桨叶独立模拟
        BladePitch1 = Clamp(BladePitch1 + (_rand.NextDouble() * 0.4 - 0.2), 0, 50);
        BladePitch2 = Clamp(BladePitch2 + (_rand.NextDouble() * 0.4 - 0.2), 0, 50);
        BladePitch3 = Clamp(BladePitch3 + (_rand.NextDouble() * 0.4 - 0.2), 0, 50);
        BladePitchAvg = (BladePitch1 + BladePitch2 + BladePitch3) / 3.0;

        // 龙门架
        GantryX = Clamp(GantryX + (_rand.NextDouble() * 4 - 2), 0, 500);
        GantryY = Clamp(GantryY + (_rand.NextDouble() * 2 - 1), 0, 300);
        GantryZ = Clamp(GantryZ + (_rand.NextDouble() * 6 - 3), 0, 800);

        // 更新历史缓冲区
        UpdateHistoryBuffers();
    }

    private void UpdateHistoryBuffers()
    {
        AddHistory(_windSpeedHistory, WindSpeed);
        AddHistory(_motorTempHistory, MotorTemp);
        AddHistory(_ratThrustHistory, RatThrust);
        AddHistory(_ratRpmHistory, RatRpm);
        AddHistory(_coolingOutTempHistory, CoolingOutTemp);
        AddHistory(_coolingReturnTempHistory, CoolingReturnTemp);
        AddHistory(_coolingFlowHistory, CoolingFlow);
        AddHistory(_bladePitch1History, BladePitch1);
        AddHistory(_pitchActualHistory, PitchActual);
        AddHistory(_yawActualHistory, YawActual);
    }

    private void AddHistory(List<double> buffer, double value)
    {
        buffer.Add(value);
        while (buffer.Count > MaxHistory) buffer.RemoveAt(0);
    }

    /// <summary>
    /// 外部服务（通讯/联锁）通过此方法更新报警
    /// </summary>
    public void OnAlarmTriggered(string description, int level, string status = "未处理")
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Alarms.Insert(0, new AlarmRecordViewModel
            {
                Level = level,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Description = description,
                Status = status,
            });
            while (Alarms.Count > 100) Alarms.RemoveAt(Alarms.Count - 1);
        });
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

/// <summary>
/// 报警记录 ViewModel
/// </summary>
public partial class AlarmRecordViewModel : ObservableObject
{
    [ObservableProperty] private int _level;
    [ObservableProperty] private string _time = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _status = "";
}
