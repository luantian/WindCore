using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class OverviewPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    // 子 ViewModel（设备控制操作）
    public PowerDrivePageViewModel PowerDrive { get; }
    public CoolingSystemPageViewModel Cooling { get; }
    public GantryControlPageViewModel GantryCtrl { get; }

    [ObservableProperty] private bool _motorStarted;
    [ObservableProperty] private bool _pumpStarted;

    public OverviewPageViewModel(DataService dataService)
    {
        _dataService = dataService;
        PowerDrive = new PowerDrivePageViewModel(dataService);
        Cooling = new CoolingSystemPageViewModel(dataService);
        GantryCtrl = new GantryControlPageViewModel(dataService);
    }

    // 仪表盘数据
    public double WindSpeed => _dataService.WindSpeed;
    public double MotorRpm => _dataService.MotorRpm;
    public double MotorTemp => _dataService.MotorTemp;
    public double RatThrust => _dataService.RatThrust;
    public double RatRpm => _dataService.RatRpm;
    public double Pitch => _dataService.PitchActual;

    // 辅助指标
    public double MotorFrequency => _dataService.MotorFrequency;
    public double CoolingFlow => _dataService.CoolingFlow;
    public double RatTorque => _dataService.RatTorque;
    public double RatPower => _dataService.RatPower;
    public double YawActual => _dataService.YawActual;

    // 动力系统状态
    public string MotorStatus => _dataService.MotorStatus;
    public double MotorWindingTemp => _dataService.MotorWindingTemp;
    public double MotorCurrent => _dataService.MotorCurrent;
    public string InverterStatus => _dataService.InverterStatus;

    // 冷却系统状态
    public double CoolingOutTemp => _dataService.CoolingOutTemp;
    public double CoolingReturnTemp => _dataService.CoolingReturnTemp;
    public double CoolingPressure => _dataService.CoolingPressure;
    public string PumpStatus => _dataService.PumpStatus;

    // 台架状态
    public double PitchSetpoint => _dataService.PitchSetpoint;
    public double PitchActual => _dataService.PitchActual;
    public double YawSetpoint => _dataService.YawSetpoint;
    public double YawActualVal => _dataService.YawActual;
    public double PitchDeviation => _dataService.PitchDeviation;
    public double YawDeviation => _dataService.YawDeviation;

    // RAT 状态
    public string RatMode => _dataService.RatMode;
    public double ThrustDeviation => 15; // TODO: 从真实数据计算
    public double SampleRate => 1000;
    public string SensorStatus => _dataService.SensorStatus;

    // 报警列表
    public ObservableCollection<AlarmRecordViewModel> Alarms => _dataService.Alarms;

    /// <summary>
    /// 报警等级显示文本
    /// </summary>
    public static string GetLevelText(int level) => level switch { 1 => "一级", 2 => "二级", _ => "三级" };

    /// <summary>
    /// 报警等级颜色
    /// </summary>
    public static string GetLevelColor(int level) => level switch { 1 => "#E34D59", 2 => "#ED7B2F", _ => "#0052D9" };

    /// <summary>
    /// 报警状态背景
    /// </summary>
    public static string GetStatusBg(int level, string status)
    {
        if (status == "已恢复") return "#F0F9EB";
        if (status == "已确认") return "#F0F5FF";
        return "#FFF1F0";
    }

    /// <summary>
    /// 报警状态文本颜色
    /// </summary>
    public static string GetStatusTextColor(int level, string status)
    {
        if (status == "已恢复") return "#2BA471";
        if (status == "已确认") return "#0052D9";
        return "#E34D59";
    }

    // ===== 高频操作命令 =====
    [RelayCommand]
    private void DoStartMotor() => PowerDrive.StartMotor();
    public void StartMotor() => DoStartMotorCommand.Execute(null);

    [RelayCommand]
    private void DoStopMotor() => PowerDrive.StopMotor();
    public void StopMotor() => DoStopMotorCommand.Execute(null);

    [RelayCommand]
    private void DoStartPump() => Cooling.StartPump();
    public void StartPump() => DoStartPumpCommand.Execute(null);

    [RelayCommand]
    private void DoStopPump() => Cooling.StopPump();
    public void StopPump() => DoStopPumpCommand.Execute(null);

    [RelayCommand]
    private void DoPitchHome() => GantryCtrl.PitchHome();
    public void PitchHome() => DoPitchHomeCommand.Execute(null);

    [RelayCommand]
    private void DoYawHome() => GantryCtrl.YawHome();
    public void YawHome() => DoYawHomeCommand.Execute(null);

    [RelayCommand]
    private void SetPitchAngle() => GantryCtrl.SetPitchAngleCommand.Execute(null);

    [RelayCommand]
    private void SetYawAngle() => GantryCtrl.SetYawAngleCommand.Execute(null);

    // 通知所有属性变化（供定时器调用）
    public void Refresh()
    {
        OnPropertyChanged(nameof(WindSpeed));
        OnPropertyChanged(nameof(MotorRpm));
        OnPropertyChanged(nameof(MotorTemp));
        OnPropertyChanged(nameof(RatThrust));
        OnPropertyChanged(nameof(RatRpm));
        OnPropertyChanged(nameof(Pitch));
        OnPropertyChanged(nameof(MotorFrequency));
        OnPropertyChanged(nameof(CoolingFlow));
        OnPropertyChanged(nameof(RatTorque));
        OnPropertyChanged(nameof(RatPower));
        OnPropertyChanged(nameof(YawActual));
        OnPropertyChanged(nameof(MotorStatus));
        OnPropertyChanged(nameof(MotorWindingTemp));
        OnPropertyChanged(nameof(MotorCurrent));
        OnPropertyChanged(nameof(InverterStatus));
        OnPropertyChanged(nameof(CoolingOutTemp));
        OnPropertyChanged(nameof(CoolingReturnTemp));
        OnPropertyChanged(nameof(CoolingPressure));
        OnPropertyChanged(nameof(PumpStatus));
        OnPropertyChanged(nameof(PitchSetpoint));
        OnPropertyChanged(nameof(PitchActual));
        OnPropertyChanged(nameof(YawSetpoint));
        OnPropertyChanged(nameof(YawActualVal));
        OnPropertyChanged(nameof(PitchDeviation));
        OnPropertyChanged(nameof(YawDeviation));
        OnPropertyChanged(nameof(RatMode));
        OnPropertyChanged(nameof(SensorStatus));
        OnPropertyChanged(nameof(Alarms));

        // 同步子 ViewModel 的 PID 更新
        PowerDrive.UpdatePID(PowerDrive.WindSpeed);
    }
}
