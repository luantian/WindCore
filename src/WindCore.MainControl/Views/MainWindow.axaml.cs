using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using IconPacks.Avalonia.FontAwesome;
using WindCore.MainControl.Controls;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Views;

public partial class MainWindow : Window
{
    private readonly LoginView _loginView;
    private readonly Toast _toast;

    public MainWindow()
    {
        InitializeComponent();
        _loginView = this.FindControl<LoginView>("loginView")!;
        _toast = this.FindControl<Toast>("toast")!;

        // Build navigation tree using the SidebarNav data model
        var navSections = new List<NavSection>
        {
            new() { Label = "系统总览", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.GaugeSolid,
                Items = { new NavItem { Label = "系统总览", Key = "overview", Kind = PackIconFontAwesomeKind.GaugeSolid } } },

            new() { Label = "设备控制", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.GearsSolid,
                Items = { new NavItem { Label = "设备控制", Key = "deviceControl", Kind = PackIconFontAwesomeKind.GearsSolid } } },

            new() { Label = "试验数据", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.FlaskSolid,
                Items = { new NavItem { Label = "试验数据", Key = "testData", Kind = PackIconFontAwesomeKind.FlaskSolid } } },

            new() { Label = "报警管理", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.BellRegular,
                Items = { new NavItem { Label = "报警管理", Key = "alarmManagement", Kind = PackIconFontAwesomeKind.BellRegular } } },

            new() { Label = "流程图", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.DiagramProjectSolid,
                Items = { new NavItem { Label = "流程图", Key = "flowchart", Kind = PackIconFontAwesomeKind.DiagramProjectSolid } } },

            new() { Label = "波形显示", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.ChartAreaSolid,
                Items = { new NavItem { Label = "波形显示", Key = "waveform", Kind = PackIconFontAwesomeKind.ChartAreaSolid } } },

            new() { Label = "系统设置", Level = 1, IsLeaf = true, Icon = PackIconFontAwesomeKind.ScrewdriverWrenchSolid,
                Items = { new NavItem { Label = "系统设置", Key = "settings", Kind = PackIconFontAwesomeKind.ScrewdriverWrenchSolid } } },
        };

        sidebarNav.Sections = navSections;
        sidebarNav.ItemClicked += (_, key) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.NavigateToCommand.Execute(key);
            }
        };

        var vm = new MainWindowViewModel();
        vm.OnLoginSuccess += (_, role) =>
        {
            try
            {
                // 第一帧：先隐藏登录框
                _loginView.Opacity = 0;
                _loginView.IsVisible = false;

                // 下一帧：再调整窗口，确保登录框已不可见
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Width = 1920;
                    Height = 1040;
                    var screen = Screens.Primary;
                    if (screen != null)
                    {
                        Position = new Avalonia.PixelPoint(
                            (screen.WorkingArea.Width - (int)Width) / 2,
                            (screen.WorkingArea.Height - (int)Height) / 2);
                    }
                    _toast.Show($"欢迎回来，角色：{role}", ToastType.Success);
                });
            }
            catch (Exception ex)
            {
                File.AppendAllText("login_debug.log", $"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}\n");
            }
        };
        vm.OnLoginError += msg =>
        {
            _toast.Show(msg, ToastType.Error);
        };

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedNavItem))
            {
                sidebarNav.SelectedKey = vm.SelectedNavItem;
            }
        };

        DataContext = vm;
    }

    public void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        _toast.Show(message, type, durationMs);
    }
}
