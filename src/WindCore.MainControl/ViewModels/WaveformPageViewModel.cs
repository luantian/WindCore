using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.ViewModels;

/// <summary>
/// 波形图页面 ViewModel
/// </summary>
public partial class WaveformPageViewModel : ViewModelBase
{
    private readonly DataService _ds;

    public List<ChannelInfo> AvailableChannels { get; } = new()
    {
        new("风速", "#0052D9"),
        new("电机温度", "#2BA471"),
        new("RAT推力", "#ED7B2F"),
        new("RAT转速", "#E34D59"),
        new("出水温度", "#722ED1"),
        new("回水温度", "#13C2C2"),
        new("冷却流量", "#FA8C16"),
        new("桨叶#1", "#0052D9"),
        new("俯仰角", "#2BA471"),
        new("侧滑角", "#ED7B2F"),
    };

    [ObservableProperty] private ObservableCollection<ChannelInfo> _selectedChannels = new();
    [ObservableProperty] private bool _isRecording;

    public WaveformPageViewModel(DataService dataService)
    {
        _ds = dataService;
    }

    public IReadOnlyList<double>? GetDataForChannel(string channelName)
    {
        return channelName switch
        {
            "风速" => _ds.WindSpeedHistory,
            "电机温度" => _ds.MotorTempHistory,
            "RAT推力" => _ds.RatThrustHistory,
            "RAT转速" => _ds.RatRpmHistory,
            "出水温度" => _ds.CoolingOutTempHistory,
            "回水温度" => _ds.CoolingReturnTempHistory,
            "冷却流量" => _ds.CoolingFlowHistory,
            "桨叶#1" => _ds.BladePitch1History,
            "俯仰角" => _ds.PitchActualHistory,
            "侧滑角" => _ds.YawActualHistory,
            _ => null,
        };
    }

    public WaveStats? GetStats(string channelName)
    {
        var data = GetDataForChannel(channelName);
        if (data == null || data.Count == 0) return null;

        double min = data.Min();
        double max = data.Max();
        double avg = data.Average();
        return new WaveStats(min, max, avg, max - min, (max - min) / 2.0);
    }

    [RelayCommand]
    private void ToggleChannel(ChannelInfo ch)
    {
        if (SelectedChannels.Contains(ch))
            SelectedChannels.Remove(ch);
        else
            SelectedChannels.Add(ch);
    }

    [RelayCommand]
    private void StartRecording() => IsRecording = true;

    [RelayCommand]
    private void StopRecording() => IsRecording = false;

    [RelayCommand]
    private void Clear()
    {
        SelectedChannels.Clear();
    }
}

public class ChannelInfo(string name, string color)
{
    public string Name { get; } = name;
    public string Color { get; } = color;
    public bool IsSelected { get; set; }

    public override string ToString() => Name;
    public override bool Equals(object? obj) => obj is ChannelInfo other && other.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
}

public record WaveStats(double Min, double Max, double Avg, double PeakToPeak, double Amplitude);
