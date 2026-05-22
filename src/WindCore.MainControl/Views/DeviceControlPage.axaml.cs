using System;
using Avalonia.Controls;
using Avalonia.Threading;
using WindCore.MainControl.Controls;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Views;

public partial class DeviceControlPage : UserControl
{
    private readonly DispatcherTimer? _pidTimer;

    public DeviceControlPage()
    {
        this.InitializeComponent();

        if (DataContext is DeviceControlPageViewModel vm)
        {
            _pidTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _pidTimer.Tick += (_, _) => vm.PowerDrive.UpdatePID(vm.PowerDrive.WindSpeed);
        }

        Loaded += (_, _) => _pidTimer?.Start();
        Unloaded += (_, _) => _pidTimer?.Stop();
    }

    private void OnStartMotorClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not DeviceControlPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要启动电机吗？", () => vm.PowerDrive.StartMotor());
    }

    private void OnStopMotorClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not DeviceControlPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要停止电机吗？", () => vm.PowerDrive.StopMotor());
    }

    private void OnStartPumpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not DeviceControlPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要启动供水泵吗？", () => vm.Cooling.StartPump());
    }

    private void OnStopPumpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not DeviceControlPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要停止供水泵吗？", () => vm.Cooling.StopPump());
    }

    private void OnPitchHomeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not DeviceControlPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要将俯仰角回零吗？此操作将清除当前角度设定。", () => vm.GantryCtrl.PitchHome());
    }

    private void OnYawHomeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not DeviceControlPageViewModel vm) return;
        confirmDialog.Show("确认操作", "确定要将侧滑角回零吗？此操作将清除当前角度设定。", () => vm.GantryCtrl.YawHome());
    }
}
