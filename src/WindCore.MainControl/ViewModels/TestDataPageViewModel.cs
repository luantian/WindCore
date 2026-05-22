using CommunityToolkit.Mvvm.ComponentModel;

namespace WindCore.MainControl.ViewModels;

public partial class TestDataPageViewModel : ObservableObject
{
    public RatPageViewModel Rat { get; }
    public GantryPageViewModel Gantry { get; }
    public BladePitchPageViewModel BladePitch { get; }

    public TestDataPageViewModel(DataService dataService)
    {
        Rat = new RatPageViewModel(dataService);
        Gantry = new GantryPageViewModel(dataService);
        BladePitch = new BladePitchPageViewModel(dataService);
    }
}
