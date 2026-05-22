using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Views;

public partial class WaveformPage : UserControl
{
    private DispatcherTimer? _refreshTimer;
    private ScottPlot.Avalonia.AvaPlot? _avaPlot;

    public WaveformPage()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _refreshTimer?.Stop();

        if (DataContext is not WaveformPageViewModel vm) return;

        // Initialize ScottPlot
        _avaPlot = this.FindControl<ScottPlot.Avalonia.AvaPlot>("wavePlot");
        if (_avaPlot != null)
        {
            _avaPlot.Plot.Title("");
            _avaPlot.Plot.XLabel("时间 (s)");
            _avaPlot.Plot.YLabel("");
            _avaPlot.Plot.Legend.IsVisible = true;
            _avaPlot.Refresh();
        }

        // Sync ListBox selection to ViewModel
        var listBox = this.FindControl<ListBox>("channelListBox");
        if (listBox != null)
        {
            listBox.SelectionChanged += (_, args) =>
            {
                if (args is not SelectionChangedEventArgs selectionArgs) return;
                SyncSelection(vm, listBox);
            };
        }

        // Refresh every 500ms
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _refreshTimer.Tick += (_, _) => RefreshPlot(vm);
        _refreshTimer.Start();
    }

    private void SyncSelection(WaveformPageViewModel vm, ListBox listBox)
    {
        vm.SelectedChannels.Clear();
        var selected = listBox.SelectedItems;
        if (selected == null) return;
        foreach (var item in selected)
        {
            if (item is ChannelInfo ch)
                vm.SelectedChannels.Add(ch);
        }
    }

    private void RefreshPlot(WaveformPageViewModel vm)
    {
        if (_avaPlot == null) return;

        _avaPlot.Plot.Clear();

        int ci = 0;
        foreach (var ch in vm.AvailableChannels)
        {
            if (!vm.SelectedChannels.Contains(ch))
            {
                ci++;
                continue;
            }

            var data = vm.GetDataForChannel(ch.Name);
            if (data == null || data.Count == 0)
            {
                ci++;
                continue;
            }

            int count = data.Count;
            double[] xValues = new double[count];
            double[] yValues = new double[count];
            for (int i = 0; i < count; i++)
            {
                xValues[i] = i * 0.1;
                yValues[i] = data[i];
            }

            var color = ScottPlot.Color.FromHex(ch.Color);
            var scatter = _avaPlot.Plot.Add.Scatter(xValues, yValues);
            scatter.LineColor = color;
            scatter.LineWidth = 1.5f;
            scatter.LegendText = ch.Name;

            ci++;
        }

        _avaPlot.Plot.Legend.IsVisible = ci > 0;
        _avaPlot.Refresh();

        UpdateStats(vm);
    }

    private void UpdateStats(WaveformPageViewModel vm)
    {
        var statsData = new List<StatsRow>();

        foreach (var ch in vm.SelectedChannels)
        {
            var stats = vm.GetStats(ch.Name);
            if (stats != null)
            {
                statsData.Add(new StatsRow(ch.Name, stats.Min, stats.Max, stats.PeakToPeak, stats.Avg, stats.Amplitude));
            }
        }

        var grid = this.FindControl<DataGrid>("statsGrid");
        if (grid != null) grid.ItemsSource = statsData;
    }
}

public class StatsRow(string name, double min, double max, double peakToPeak, double avg, double amplitude)
{
    public string Name { get; } = name;
    public double Min { get; } = min;
    public double Max { get; } = max;
    public double PeakToPeak { get; } = peakToPeak;
    public double Avg { get; } = avg;
    public double Amplitude { get; } = amplitude;
}
