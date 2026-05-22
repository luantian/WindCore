using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindCore.Core.PID;

namespace WindCore.MainControl.ViewModels;

public partial class PowerDrivePageViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly WindSpeedPIDController _pid;

    [ObservableProperty] private double _targetWindSpeed;
    [ObservableProperty] private double _targetRpm;
    [ObservableProperty] private bool _motorRunning;
    [ObservableProperty] private double _kp = 1.0;
    [ObservableProperty] private double _ki = 0.1;
    [ObservableProperty] private double _kd = 0.01;
    [ObservableProperty] private double _outputMin;
    [ObservableProperty] private double _outputMax = 100;
    [ObservableProperty] private double _filterAlpha = 0.2;
    [ObservableProperty] private double _currentOutput;
    [ObservableProperty] private string _pidStatus = "未运行";

    // 实时显示
    public double WindSpeed => _dataService.WindSpeed;
    public double MotorRpm => _dataService.MotorRpm;
    public double MotorTemp => _dataService.MotorTemp;
    public double MotorCurrent => _dataService.MotorCurrent;
    public double MotorFrequency => _dataService.MotorFrequency;
    public string MotorStatus => _dataService.MotorStatus;
    public string InverterStatus => _dataService.InverterStatus;
    public double MotorWindingTemp => _dataService.MotorWindingTemp;

    public PowerDrivePageViewModel(DataService dataService)
    {
        _dataService = dataService;
        _pid = new WindSpeedPIDController(new PIDConfig { Kp = 1.0, Ki = 0.1, Kd = 0.01, OutputMin = 0, OutputMax = 100, InitialSetpoint = 80 });
        _targetWindSpeed = 80;
        _targetRpm = 1400;
    }

    [RelayCommand]
    public void StartMotor()
    {
        MotorRunning = true;
        PidStatus = "运行中";
        _dataService.MotorStatus = "运行中";
    }

    [RelayCommand]
    public void StopMotor()
    {
        MotorRunning = false;
        PidStatus = "已停止";
        _dataService.MotorStatus = "已停止";
        _pid.Reset();
    }

    [RelayCommand]
    private void ApplyPID()
    {
        _pid.UpdateConfig(new PIDConfig
        {
            Kp = Kp,
            Ki = Ki,
            Kd = Kd,
            OutputMin = OutputMin,
            OutputMax = OutputMax,
            DerivativeFilterAlpha = FilterAlpha,
            InitialSetpoint = TargetWindSpeed,
        });
        PidStatus = "参数已应用";
    }

    public void UpdatePID(double measuredWindSpeed)
    {
        if (MotorRunning)
        {
            _pid.Setpoint = TargetWindSpeed;
            CurrentOutput = _pid.Compute(measuredWindSpeed, 0.1);
            OnPropertyChanged(nameof(WindSpeed));
            OnPropertyChanged(nameof(MotorRpm));
            OnPropertyChanged(nameof(MotorTemp));
            OnPropertyChanged(nameof(MotorCurrent));
            OnPropertyChanged(nameof(MotorFrequency));
            OnPropertyChanged(nameof(MotorStatus));
            OnPropertyChanged(nameof(InverterStatus));
            OnPropertyChanged(nameof(MotorWindingTemp));
        }
    }
}
