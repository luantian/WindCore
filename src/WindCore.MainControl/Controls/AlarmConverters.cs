using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WindCore.MainControl.Controls;

public class AlarmLevelColorConverter : IValueConverter
{
    public static AlarmLevelColorConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int level)
        {
            return level switch { 1 => new SolidColorBrush(Color.Parse("#E34D59")), 2 => new SolidColorBrush(Color.Parse("#ED7B2F")), _ => new SolidColorBrush(Color.Parse("#0052D9")) };
        }
        return new SolidColorBrush(Color.Parse("#86909C"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class AlarmStatusBgConverter : IValueConverter
{
    public static AlarmStatusBgConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s switch { "已恢复" => new SolidColorBrush(Color.Parse("#F0F9EB")), "已确认" => new SolidColorBrush(Color.Parse("#F0F5FF")), _ => new SolidColorBrush(Color.Parse("#FFF1F0")) };
        }
        return new SolidColorBrush(Color.Parse("#FFF1F0"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SubsystemToggleTextConverter : IValueConverter
{
    public static SubsystemToggleTextConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return b ? "投入" : "退出";
        return "退出";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class AlarmStatusTextColorConverter : IValueConverter
{
    public static AlarmStatusTextColorConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s switch { "已恢复" => new SolidColorBrush(Color.Parse("#2BA471")), "已确认" => new SolidColorBrush(Color.Parse("#0052D9")), _ => new SolidColorBrush(Color.Parse("#E34D59")) };
        }
        return new SolidColorBrush(Color.Parse("#E34D59"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CommStatusColorConverter : IValueConverter
{
    public static CommStatusColorConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s switch
            {
                "已连接" or "正常" => new SolidColorBrush(Color.Parse("#2BA471")),
                "通讯中" => new SolidColorBrush(Color.Parse("#ED7B2F")),
                _ => new SolidColorBrush(Color.Parse("#E34D59")),
            };
        }
        return new SolidColorBrush(Color.Parse("#E34D59"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
