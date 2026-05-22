using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class CoolingSystemPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    [ObservableProperty] private double _valveOpening;
    [ObservableProperty] private double _targetTemp;
    [ObservableProperty] private bool _pumpRunning;

    public double CoolingTemp => _dataService.CoolingTemp;
    public double CoolingOutTemp => _dataService.CoolingOutTemp;
    public double CoolingReturnTemp => _dataService.CoolingReturnTemp;
    public double CoolingFlow => _dataService.CoolingFlow;
    public double CoolingPressure => _dataService.CoolingPressure;
    public string PumpStatus => _dataService.PumpStatus;
    public double TankLevel => _dataService.TankLevel;

    public CoolingSystemPageViewModel(DataService dataService)
    {
        _dataService = dataService;
        _dataService.PropertyChanged += OnDataServicePropertyChanged;
        _valveOpening = 60;
        _targetTemp = 25;
        _pumpRunning = true;
    }

    private void OnDataServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DataService.CoolingTemp): OnPropertyChanged(nameof(CoolingTemp)); break;
            case nameof(DataService.CoolingOutTemp): OnPropertyChanged(nameof(CoolingOutTemp)); break;
            case nameof(DataService.CoolingReturnTemp): OnPropertyChanged(nameof(CoolingReturnTemp)); break;
            case nameof(DataService.CoolingFlow): OnPropertyChanged(nameof(CoolingFlow)); break;
            case nameof(DataService.CoolingPressure): OnPropertyChanged(nameof(CoolingPressure)); break;
            case nameof(DataService.PumpStatus): OnPropertyChanged(nameof(PumpStatus)); break;
            case nameof(DataService.TankLevel): OnPropertyChanged(nameof(TankLevel)); break;
        }
    }

    [RelayCommand]
    public void StartPump()
    {
        PumpRunning = true;
        _dataService.PumpStatus = "运行中";
    }

    [RelayCommand]
    public void StopPump()
    {
        PumpRunning = false;
        _dataService.PumpStatus = "已停止";
    }

    [RelayCommand]
    private void ApplyValve()
    {
        // TODO: Send valve opening to PLC via Modbus
    }

    [RelayCommand]
    private void ApplyTargetTemp()
    {
        // TODO: Send target temp to cooling controller
    }
}
