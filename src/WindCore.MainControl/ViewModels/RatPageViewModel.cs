using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class RatPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    [ObservableProperty] private string _ratMode = "恒推力";
    [ObservableProperty] private double _targetThrust = 1000;
    [ObservableProperty] private double _targetRpm = 5000;
    [ObservableProperty] private double _targetTorque = 50;

    public string[] ModeOptions { get; } = { "恒推力", "恒转速", "恒扭矩" };

    public double RatThrust => _dataService.RatThrust;
    public double RatRpm => _dataService.RatRpm;
    public double RatTorque => _dataService.RatTorque;
    public double RatPower => _dataService.RatPower;
    public string SensorStatus => _dataService.SensorStatus;

    public RatPageViewModel(DataService dataService)
    {
        _dataService = dataService;
        _dataService.PropertyChanged += OnDataServicePropertyChanged;
    }

    private void OnDataServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DataService.RatThrust): OnPropertyChanged(nameof(RatThrust)); break;
            case nameof(DataService.RatRpm): OnPropertyChanged(nameof(RatRpm)); break;
            case nameof(DataService.RatTorque): OnPropertyChanged(nameof(RatTorque)); break;
            case nameof(DataService.RatPower): OnPropertyChanged(nameof(RatPower)); break;
            case nameof(DataService.SensorStatus): OnPropertyChanged(nameof(SensorStatus)); break;
        }
    }

    [RelayCommand]
    private void ApplyRatMode()
    {
        // 通过 TCP 下发 RAT 模式及目标参数
        // TODO: 发送 RAT 控制命令
    }

    [RelayCommand]
    private void ApplyTargetThrust()
    {
        // TODO: 下发目标推力
    }

    [RelayCommand]
    private void ApplyTargetRpm()
    {
        // TODO: 下发目标转速
    }

    [RelayCommand]
    private void ApplyTargetTorque()
    {
        // TODO: 下发目标扭矩
    }
}
