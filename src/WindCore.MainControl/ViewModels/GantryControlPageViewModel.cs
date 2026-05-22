using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class GantryControlPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    // 俯仰角
    [ObservableProperty] private double _pitchSetpoint;
    [ObservableProperty] private double _pitchSpeed;

    // 侧滑角
    [ObservableProperty] private double _yawSetpoint;
    [ObservableProperty] private double _yawSpeed;

    public double PitchActual => _dataService.PitchActual;
    public double YawActual => _dataService.YawActual;
    public double PitchDeviation => _dataService.PitchDeviation;
    public double YawDeviation => _dataService.YawDeviation;

    public GantryControlPageViewModel(DataService dataService)
    {
        _dataService = dataService;
        _dataService.PropertyChanged += OnDataServicePropertyChanged;
        _pitchSetpoint = 0;
        _pitchSpeed = 5;
        _yawSetpoint = 0;
        _yawSpeed = 5;
    }

    private void OnDataServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DataService.PitchActual): OnPropertyChanged(nameof(PitchActual)); break;
            case nameof(DataService.YawActual): OnPropertyChanged(nameof(YawActual)); break;
            case nameof(DataService.PitchDeviation): OnPropertyChanged(nameof(PitchDeviation)); break;
            case nameof(DataService.YawDeviation): OnPropertyChanged(nameof(YawDeviation)); break;
        }
    }

    [RelayCommand]
    private void SetPitchAngle()
    {
        _dataService.PitchSetpoint = PitchSetpoint;
    }

    [RelayCommand]
    private void SetYawAngle()
    {
        _dataService.YawSetpoint = YawSetpoint;
    }

    [RelayCommand]
    public void PitchHome()
    {
        PitchSetpoint = 0;
        _dataService.PitchSetpoint = 0;
    }

    [RelayCommand]
    public void YawHome()
    {
        YawSetpoint = 0;
        _dataService.YawSetpoint = 0;
    }
}
