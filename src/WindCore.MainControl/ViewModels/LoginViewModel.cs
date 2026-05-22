using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindCore.MainControl.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "admin";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _hasError;

    // 用户名 → 角色映射 (临时, 后续从数据库查询)
    private static readonly Dictionary<string, string> RoleMap = new()
    {
        { "admin", "管理员" },
        { "eng", "工程师" },
        { "op", "操作员" },
        { "view", "观察员" },
    };

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            HasError = true;
            ErrorMessage = "请输入用户名";
            return;
        }

        // 根据用户名确定角色
        if (!RoleMap.TryGetValue(Username, out var role))
        {
            HasError = true;
            ErrorMessage = "用户名不存在";
            return;
        }

        // 验证密码
        if (Password != GetDefaultPassword(Username))
        {
            HasError = true;
            ErrorMessage = "用户名或密码错误";
            OnLoginFailed?.Invoke("用户名或密码错误");
            return;
        }

        HasError = false;
        ErrorMessage = "";
        OnLoginSuccess?.Invoke(Username, role);
    }

    [RelayCommand]
    private void Cancel()
    {
        Username = "";
        Password = "";
        HasError = false;
        ErrorMessage = "";
    }

    private static string GetDefaultPassword(string username) => username switch
    {
        "admin" => "admin",
        "eng" => "eng123",
        "op" => "op123",
        "view" => "view123",
        _ => "",
    };

    public event System.Action<string, string>? OnLoginSuccess;
    public event System.Action<string>? OnLoginFailed;
}
