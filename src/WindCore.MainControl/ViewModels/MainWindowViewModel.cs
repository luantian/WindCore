using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindCore.MainControl.Services;
using WindCore.MainControl.ViewModels;
using WindCore.MainControl.Views;

namespace WindCore.MainControl.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public DataService DataService { get; }

    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private bool _isLoginVisible = true;
    [ObservableProperty] private string _currentUser = "";
    [ObservableProperty] private string _currentRole = "";
    [ObservableProperty] private object? _selectedNavPage;
    [ObservableProperty] private string _selectedNavItem = "";

    public LoginViewModel LoginViewModel { get; }

    public event System.Action<string, string>? OnLoginSuccess;
    public event System.Action<string>? OnLoginError;

    private readonly Dictionary<string, Func<object>> _pageFactories;
    private readonly SettingsPanelViewModel _settingsViewModel;
    private CommunicationService? _commService;
    private InterlockService? _interlockService;
    private StorageService? _storageService;

    public MainWindowViewModel()
    {
        DataService = new DataService();
        LoginViewModel = new LoginViewModel();
        _settingsViewModel = new SettingsPanelViewModel();
        File.AppendAllText("login_debug.log", $"[{DateTime.Now:HH:mm:ss}] MainWindowViewModel ctor: subscribing to LoginViewModel.OnLoginSuccess\n");
        LoginViewModel.OnLoginSuccess += HandleLoginSuccess;
        LoginViewModel.OnLoginFailed += msg => { File.AppendAllText("login_debug.log", $"[{DateTime.Now:HH:mm:ss}] MainWindowViewModel received OnLoginFailed: {msg}\n"); OnLoginError?.Invoke(msg); };

        // Wire Settings connect/disconnect to CommunicationService
        _settingsViewModel.ConnectRequested += (ip, port) =>
        {
            if (_commService != null)
            {
                bool ok = _commService.Connect(ip, port);
                _settingsViewModel.IsConnected = ok;
                _settingsViewModel.CommStatus = ok ? "已连接" : "连接失败";
            }
        };
        _settingsViewModel.DisconnectRequested += () =>
        {
            _commService?.Disconnect();
            _settingsViewModel.IsConnected = false;
            _settingsViewModel.CommStatus = "未连接";
        };

        _pageFactories = new Dictionary<string, Func<object>>
        {
            { "overview", () => new OverviewPage { DataContext = new OverviewPageViewModel(DataService) } },
            { "deviceControl", () => new DeviceControlPage { DataContext = new DeviceControlPageViewModel(DataService) } },
            { "testData", () => new TestDataPage { DataContext = new TestDataPageViewModel(DataService) } },
            { "alarmManagement", () => new AlarmManagementPage { DataContext = new AlarmManagementPageViewModel(DataService) } },
            { "flowchart", () => new FlowchartPage { DataContext = new FlowchartPageViewModel(DataService) } },
            { "waveform", () => new WaveformPage { DataContext = new WaveformPageViewModel(DataService) } },
            { "settings", () => new SettingsPage { DataContext = _settingsViewModel } },
            { "test", () => new TestPage() },
        };
    }

    [RelayCommand]
    private void NavigateTo(string navKey)
    {
        if (_pageFactories.TryGetValue(navKey, out var factory))
        {
            SelectedNavItem = navKey;
            SelectedNavPage = factory();
        }
    }

    private void HandleLoginSuccess(string username, string role)
    {
        File.AppendAllText("login_debug.log", $"[{DateTime.Now:HH:mm:ss}] HandleLoginSuccess called! user={username}, role={role}\n");
        CurrentUser = username;
        CurrentRole = role;
        IsLoggedIn = true;
        IsLoginVisible = false;
        File.AppendAllText("login_debug.log", $"[{DateTime.Now:HH:mm:ss}] About to invoke OnLoginSuccess. Subscribers: {OnLoginSuccess != null}\n");

        DataService.Start();

        // 启动通讯服务
        _commService = new CommunicationService(DataService);

        // 启动联锁服务
        _interlockService = new InterlockService(DataService, _settingsViewModel);
        _interlockService.Start();

        // 启动存储服务
        _storageService = new StorageService(DataService, _settingsViewModel);

        SelectedNavItem = "overview";
        SelectedNavPage = new OverviewPage { DataContext = new OverviewPageViewModel(DataService) };

        OnLoginSuccess?.Invoke(username, role);
        File.AppendAllText("login_debug.log", $"[{DateTime.Now:HH:mm:ss}] OnLoginSuccess returned\n");
    }

    /// <summary>
    /// 退出时释放所有服务
    /// </summary>
    public void Shutdown()
    {
        _storageService?.Dispose();
        _interlockService?.Dispose();
        _commService?.Dispose();
        DataService.Stop();
    }
}
