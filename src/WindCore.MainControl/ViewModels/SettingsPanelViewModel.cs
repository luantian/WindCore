using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class SettingsPanelViewModel : ViewModelBase
{
    public List<string> ProtocolOptions { get; } = ["Modbus TCP", "Modbus RTU", "OPC"];

    // ===== Modbus 通讯设置 =====
    [ObservableProperty] private string _modbusProtocol = "Modbus TCP";
    [ObservableProperty] private int _commPeriodMs = 100;
    [ObservableProperty] private string _plcIp = "192.168.1.100";
    [ObservableProperty] private int _plcPort = 502;
    [ObservableProperty] private string _collectorIp = "192.168.1.200";
    [ObservableProperty] private int _collectorPort = 8080;
    [ObservableProperty] private bool _isConnected;

    // 通讯状态
    [ObservableProperty] private string _commStatus = "未连接";

    // ===== 动力系统风速控制 =====
    [ObservableProperty] private double _targetWindSpeed = 80;
    [ObservableProperty] private double _targetMotorRpm = 1400;
    [ObservableProperty] private double _pidKp = 1.0;
    [ObservableProperty] private double _pidKi = 0.1;
    [ObservableProperty] private double _pidKd = 0.01;
    [ObservableProperty] private double _pidOutputMin = 0;
    [ObservableProperty] private double _pidOutputMax = 100;
    [ObservableProperty] private double _pidFilterAlpha = 0.2;
    [ObservableProperty] private double _pidIntegralMax = 100;

    // ===== 冷却系统 =====
    [ObservableProperty] private double _valveOpening = 60;
    [ObservableProperty] private double _targetCoolingTemp = 25;

    // ===== 台架系统 =====
    [ObservableProperty] private double _pitchAngleMin = -20;
    [ObservableProperty] private double _pitchAngleMax = 20;
    [ObservableProperty] private double _yawAngleMin = -20;
    [ObservableProperty] private double _yawAngleMax = 20;
    [ObservableProperty] private double _pitchPrecision = 0.5;
    [ObservableProperty] private double _yawPrecision = 0.5;

    // ===== 龙门架 =====
    [ObservableProperty] private double _gantryXLimit = 5;
    [ObservableProperty] private double _gantryYLimit = 5;
    [ObservableProperty] private double _gantryZLimit = 5;
    [ObservableProperty] private double _gantryAccelMax = 2;

    // ===== 负载系统 RAT =====
    [ObservableProperty] private double _ratThrustMin = 0;
    [ObservableProperty] private double _ratThrustMax = 5000;
    [ObservableProperty] private double _ratTorqueMin = 0;
    [ObservableProperty] private double _ratTorqueMax = 200;
    [ObservableProperty] private double _ratRpmMin = 0;
    [ObservableProperty] private double _ratRpmMax = 10000;

    // ===== 桨距角测量 =====
    [ObservableProperty] private double _bladePitchMin = 0;
    [ObservableProperty] private double _bladePitchMax = 50;

    // ===== 数据存储 =====
    [ObservableProperty] private string _dataPath = "C:\\WindCore\\Data";
    [ObservableProperty] private int _sampleRate = 100;
    [ObservableProperty] private string _csvFileName = "experiment_data";
    [ObservableProperty] private int _backupInterval = 24;

    // ===== 告警配置 =====
    [ObservableProperty] private bool _alarmSoundEnabled = true;
    [ObservableProperty] private bool _alarmLightEnabled = true;
    [ObservableProperty] private bool _alarmScreenEnabled = true;
    [ObservableProperty] private bool _alarmPushEnabled = true;
    [ObservableProperty] private bool _alarmLogEnabled = true;

    // ===== 安全连锁阈值 =====
    [ObservableProperty] private double _shaftTempLimit = 55;
    [ObservableProperty] private double _windingTempLimit = 55;
    [ObservableProperty] private double _shaftVibrationLimit = 50;
    [ObservableProperty] private double _tankLevelMin = 10;
    [ObservableProperty] private double _pipeTempMin = 5;
    [ObservableProperty] private double _pipePressureMax = 1.0;
    [ObservableProperty] private double _coolingTempMax = 80;
    [ObservableProperty] private double _windSpeedRangeMin = 20;
    [ObservableProperty] private double _windSpeedRangeMax = 120;
    [ObservableProperty] private double _windSpeedPrecision = 1;

    // ===== 系统参数 =====
    [ObservableProperty] private int _cpuUsageLimit = 30;
    [ObservableProperty] private int _interlockCheckMs = 100;

    // ===== Excel 配置 =====
    [ObservableProperty] private string _excelConfigPath = "";
    public List<string> ExperimentTypes { get; } = ["默认试验", "静态试验", "动态试验"];
    [ObservableProperty] private string _selectedExperimentType = "默认试验";
    [ObservableProperty] private double _lowPassCutoff = 100;
    [ObservableProperty] private int _avgFilterWindow = 5;
    [ObservableProperty] private int _medianFilterWindow = 3;

    // ===== 权限管理 =====
    public List<string> RoleOptions { get; } = ["管理员", "操作员"];
    [ObservableProperty] private string _selectedRole = "操作员";
    [ObservableProperty] private bool _permDeviceControl;
    [ObservableProperty] private bool _permDataAcquisition;
    [ObservableProperty] private bool _permDataStorage;
    [ObservableProperty] private bool _permAlarmConfig;
    [ObservableProperty] private bool _permSystemSettings;
    [ObservableProperty] private bool _permUserManagement;

    // ===== 存储模式 =====
    public List<string> StorageModeOptions { get; } = ["本地 CSV", "数据库联动", "数采联动"];
    [ObservableProperty] private string _storageMode = "本地 CSV";
    [ObservableProperty] private string _dbHost = "192.168.1.50";
    [ObservableProperty] private int _dbPort = 3306;
    [ObservableProperty] private string _dbName = "windcore_db";
    [ObservableProperty] private string _dbUser = "root";
    [ObservableProperty] private string _dbPassword = "";
    [ObservableProperty] private string _daqHost = "192.168.1.200";
    [ObservableProperty] private int _daqPort = 9090;

    // ===== 日志配置 =====
    [ObservableProperty] private string _operationLogPath = "C:\\WindCore\\Logs";
    [ObservableProperty] private bool _operationLogEnabled = true;
    [ObservableProperty] private bool _alarmLogAutoCleanEnabled;
    [ObservableProperty] private int _alarmLogCleanDays = 90;

    // ===== 波形图配置 =====
    [ObservableProperty] private double _waveRefreshRate = 10;
    [ObservableProperty] private string _waveDefaultChannel = "风速";

    // ===== 试验管理 =====
    [ObservableProperty] private string _experimentDataPath = "C:\\WindCore\\Data\\试验";
    [ObservableProperty] private int _autoTrialWarmupSec = 30;
    [ObservableProperty] private int _autoTrialSteadyStateSec = 60;
    [ObservableProperty] private bool _autoTrialAutoSave = true;

    // ===== 界面设置 =====
    public List<string> ThemeOptions { get; } = ["浅色", "深色"];
    [ObservableProperty] private string _theme = "浅色";
    [ObservableProperty] private double _sidebarWidth = 200;
    [ObservableProperty] private int _waveRefreshRateMs = 100;

    // ===== 用户管理 =====
    public List<string> UserList { get; } = ["admin (管理员)", "eng (工程师)", "op (操作员)", "view (观察员)"];
    [ObservableProperty] private string _selectedUser = "";
    [ObservableProperty] private string _newUsername = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _newRole = "操作员";
    public List<string> NewRoleOptions { get; } = ["管理员", "操作员"];

    public event System.Action<bool>? CloseRequested;
    public event System.Action<string>? SaveRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(false);

    [RelayCommand]
    private void Save() => SaveRequested?.Invoke("设置已保存");

    [RelayCommand]
    private void LoadExcelConfig() { }

    [RelayCommand]
    private void WriteConfigToDevice() { }

    [RelayCommand]
    private void AddExperimentType() { }

    [RelayCommand]
    private void RemoveExperimentType() { }

    [RelayCommand]
    private void SavePermissions() { }

    [RelayCommand]
    private void TestDbConnection() { }

    [RelayCommand]
    private void AddUser() { }

    [RelayCommand]
    private void RemoveUser() { }

    /// <summary>
    /// 由 MainWindowViewModel 调用，触发实际连接
    /// </summary>
    public event System.Action<string, int>? ConnectRequested;
    public event System.Action? DisconnectRequested;

    [RelayCommand]
    private void ConnectPlc() => ConnectRequested?.Invoke(PlcIp, PlcPort);

    [RelayCommand]
    private void DisconnectPlc() => DisconnectRequested?.Invoke();
}
