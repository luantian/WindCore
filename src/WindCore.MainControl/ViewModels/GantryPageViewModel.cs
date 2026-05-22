using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class GantryPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    [ObservableProperty] private double _targetX;
    [ObservableProperty] private double _targetY;
    [ObservableProperty] private double _targetZ;
    [ObservableProperty] private double _speedX = 10;
    [ObservableProperty] private double _speedY = 10;
    [ObservableProperty] private double _speedZ = 10;

    public double GantryX => _dataService.GantryX;
    public double GantryY => _dataService.GantryY;
    public double GantryZ => _dataService.GantryZ;

    public GantryPageViewModel(DataService dataService)
    {
        _dataService = dataService;
    }

    [RelayCommand]
    private void MoveToPosition()
    {
        _dataService.GantryX = TargetX;
        _dataService.GantryY = TargetY;
        _dataService.GantryZ = TargetZ;
    }

    [RelayCommand]
    private void MoveHome()
    {
        TargetX = 0; TargetY = 0; TargetZ = 0;
        _dataService.GantryX = 0;
        _dataService.GantryY = 0;
        _dataService.GantryZ = 0;
    }
}
