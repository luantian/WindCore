using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class AlarmManagementPageViewModel : ObservableObject
{
    private readonly DataService _dataService;

    public ObservableCollection<AlarmRecordViewModel> Alarms => _dataService.Alarms;

    public AlarmManagementPageViewModel(DataService dataService)
    {
        _dataService = dataService;
    }

    [RelayCommand]
    private void ClearAlarm(AlarmRecordViewModel alarm)
    {
        alarm.Status = "已确认";
    }

    [RelayCommand]
    private void ClearAllAlarms()
    {
        foreach (var alarm in _dataService.Alarms)
        {
            if (alarm.Status != "已恢复")
                alarm.Status = "已确认";
        }
    }
}
