using System;
using Avalonia.Controls;
using Avalonia.Input;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void Password_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
        {
            vm.LoginCommand.Execute(null);
        }
    }
}
