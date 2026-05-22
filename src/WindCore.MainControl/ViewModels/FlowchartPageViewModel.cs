using CommunityToolkit.Mvvm.ComponentModel;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.ViewModels;

/// <summary>
/// 流程图页面 ViewModel — 属性委托到 DataService
/// </summary>
public partial class FlowchartPageViewModel : ViewModelBase
{
    private readonly DataService _ds;

    public FlowchartPageViewModel(DataService dataService)
    {
        _ds = dataService;
    }

    // 动力系统
    public double WindSpeed => _ds.WindSpeed;
    public double MotorRpm => _ds.MotorRpm;
    public double MotorTemp => _ds.MotorTemp;
    public double MotorWindingTemp => _ds.MotorWindingTemp;
    public string MotorStatus => _ds.MotorStatus;

    // 冷却系统
    public double CoolingOutTemp => _ds.CoolingOutTemp;
    public double CoolingReturnTemp => _ds.CoolingReturnTemp;
    public double CoolingFlow => _ds.CoolingFlow;
    public double CoolingPressure => _ds.CoolingPressure;
    public double TankLevel => _ds.TankLevel;
    public string PumpStatus => _ds.PumpStatus;

    // 台架
    public double PitchActual => _ds.PitchActual;
    public double YawActual => _ds.YawActual;

    // RAT
    public double RatThrust => _ds.RatThrust;
    public double RatRpm => _ds.RatRpm;
    public double RatTorque => _ds.RatTorque;
    public double RatPower => _ds.RatPower;

    // 桨距角
    public double BladePitch1 => _ds.BladePitch1;
    public double BladePitch2 => _ds.BladePitch2;
    public double BladePitch3 => _ds.BladePitch3;
    public double BladePitchAvg => _ds.BladePitchAvg;

    // 龙门架
    public double GantryX => _ds.GantryX;
    public double GantryY => _ds.GantryY;
    public double GantryZ => _ds.GantryZ;

    // 状态色
    public string MotorColor => _ds.MotorTemp > 55 ? "#E34D59" : "#2BA471";
    public string CoolingColor => _ds.CoolingPressure > 1.0 ? "#E34D59" : "#2BA471";
    public string TankColor => _ds.TankLevel < 10 ? "#E34D59" : "#2BA471";
    public string RatColor => _ds.RatThrust > 5000 ? "#E34D59" : "#2BA471";

    public void Refresh()
    {
        OnPropertyChanged(nameof(WindSpeed));
        OnPropertyChanged(nameof(MotorRpm));
        OnPropertyChanged(nameof(MotorTemp));
        OnPropertyChanged(nameof(MotorWindingTemp));
        OnPropertyChanged(nameof(MotorStatus));
        OnPropertyChanged(nameof(CoolingOutTemp));
        OnPropertyChanged(nameof(CoolingReturnTemp));
        OnPropertyChanged(nameof(CoolingFlow));
        OnPropertyChanged(nameof(CoolingPressure));
        OnPropertyChanged(nameof(TankLevel));
        OnPropertyChanged(nameof(PumpStatus));
        OnPropertyChanged(nameof(PitchActual));
        OnPropertyChanged(nameof(YawActual));
        OnPropertyChanged(nameof(RatThrust));
        OnPropertyChanged(nameof(RatRpm));
        OnPropertyChanged(nameof(RatTorque));
        OnPropertyChanged(nameof(RatPower));
        OnPropertyChanged(nameof(BladePitch1));
        OnPropertyChanged(nameof(BladePitch2));
        OnPropertyChanged(nameof(BladePitch3));
        OnPropertyChanged(nameof(BladePitchAvg));
        OnPropertyChanged(nameof(GantryX));
        OnPropertyChanged(nameof(GantryY));
        OnPropertyChanged(nameof(GantryZ));
        OnPropertyChanged(nameof(MotorColor));
        OnPropertyChanged(nameof(CoolingColor));
        OnPropertyChanged(nameof(TankColor));
        OnPropertyChanged(nameof(RatColor));
    }
}
