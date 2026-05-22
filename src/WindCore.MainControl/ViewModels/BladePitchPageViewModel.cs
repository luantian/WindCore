using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class BladePitchPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    [ObservableProperty] private double _targetPitch = 15;

    public double BladePitch1 => _dataService.BladePitch1;
    public double BladePitch2 => _dataService.BladePitch2;
    public double BladePitch3 => _dataService.BladePitch3;
    public double BladePitchAvg => _dataService.BladePitchAvg;

    public BladePitchPageViewModel(DataService dataService)
    {
        _dataService = dataService;
        _dataService.PropertyChanged += OnDataServicePropertyChanged;
    }

    private void OnDataServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DataService.BladePitch1): OnPropertyChanged(nameof(BladePitch1)); break;
            case nameof(DataService.BladePitch2): OnPropertyChanged(nameof(BladePitch2)); break;
            case nameof(DataService.BladePitch3): OnPropertyChanged(nameof(BladePitch3)); break;
            case nameof(DataService.BladePitchAvg): OnPropertyChanged(nameof(BladePitchAvg)); break;
        }
    }

    [RelayCommand]
    private void SetTargetPitch()
    {
        // 下发目标角度至桨距角测量系统
        // TODO: 通过 TCP 发送目标角度
    }
}
