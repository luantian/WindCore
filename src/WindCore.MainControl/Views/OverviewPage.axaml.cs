using System;
using Avalonia.Controls;
using Avalonia.Threading;
using WindCore.MainControl.Controls;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Views;

public partial class OverviewPage : UserControl
{
    private readonly DispatcherTimer? _refreshTimer;

    public OverviewPage()
    {
        this.InitializeComponent();

        if (DataContext is OverviewPageViewModel vm)
        {
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _refreshTimer.Tick += (_, _) => vm.Refresh();
        }

        Loaded += (_, _) => _refreshTimer?.Start();
        Unloaded += (_, _) => _refreshTimer?.Stop();
    }

    private void OnStartMotorClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not OverviewPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要启动电机吗？", () => vm.StartMotor());
    }

    private void OnStopMotorClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not OverviewPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要停止电机吗？", () => vm.StopMotor());
    }

    private void OnStartPumpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not OverviewPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要启动供水泵吗？", () => vm.StartPump());
    }

    private void OnStopPumpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not OverviewPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要停止供水泵吗？", () => vm.StopPump());
    }

    private void OnPitchHomeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not OverviewPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要将俯仰角回零吗？此操作将清除当前角度设定。", () => vm.PitchHome());
    }

    private void OnYawHomeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not OverviewPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要将侧滑角回零吗？此操作将清除当前角度设定。", () => vm.YawHome());
    }
}
