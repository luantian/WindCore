using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindCore.MainControl.ViewModels;

public partial class RatPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    public double RatThrust => _dataService.RatThrust;
    public double RatRpm => _dataService.RatRpm;
    public double RatTorque => _dataService.RatTorque;
    public double RatPower => _dataService.RatPower;
    public string RatMode => _dataService.RatMode;
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
            case nameof(DataService.RatMode): OnPropertyChanged(nameof(RatMode)); break;
            case nameof(DataService.SensorStatus): OnPropertyChanged(nameof(SensorStatus)); break;
        }
    }
}
