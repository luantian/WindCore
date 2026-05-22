using System;
using Avalonia.Controls;
using Avalonia.Threading;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Views;

public partial class FlowchartPage : UserControl
{
    private DispatcherTimer? _refreshTimer;

    public FlowchartPage()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _refreshTimer?.Stop();

        if (DataContext is FlowchartPageViewModel vm)
        {
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _refreshTimer.Tick += (_, _) => vm.Refresh();
            _refreshTimer.Start();
        }
    }
}
