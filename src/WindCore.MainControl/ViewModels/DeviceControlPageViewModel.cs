using CommunityToolkit.Mvvm.ComponentModel;

namespace WindCore.MainControl.ViewModels;

public partial class DeviceControlPageViewModel : ObservableObject
{
    public PowerDrivePageViewModel PowerDrive { get; }
    public CoolingSystemPageViewModel Cooling { get; }
    public GantryControlPageViewModel GantryCtrl { get; }

    public DeviceControlPageViewModel(DataService dataService)
    {
        PowerDrive = new PowerDrivePageViewModel(dataService);
        Cooling = new CoolingSystemPageViewModel(dataService);
        GantryCtrl = new GantryControlPageViewModel(dataService);
    }
}
