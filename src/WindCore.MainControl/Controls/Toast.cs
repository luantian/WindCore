using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;

namespace WindCore.MainControl.Controls;

public class Toast : ContentControl
{
    private Border? _toastBorder;
    private DispatcherTimer? _timer;

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<Toast, string>(nameof(Message));

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly StyledProperty<ToastType> ToastTypeProperty =
        AvaloniaProperty.Register<Toast, ToastType>(nameof(ToastType));

    public ToastType ToastType
    {
        get => GetValue(ToastTypeProperty);
        set => SetValue(ToastTypeProperty, value);
    }

    static Toast()
    {
        AffectsRender<Toast>(ToastTypeProperty);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _toastBorder = e.NameScope.Find<Border>("toastBorder");
    }

    public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        Message = message;
        ToastType = type;
        IsVisible = true;

        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMs)
        };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            IsVisible = false;
        };
        _timer.Start();
    }

    public void Hide()
    {
        _timer?.Stop();
        IsVisible = false;
    }

    internal static IBrush GetBackground(ToastType type) => type switch
    {
        ToastType.Success => new SolidColorBrush(0xFF4CAF50),
        ToastType.Error   => new SolidColorBrush(0xFFD9534F),
        ToastType.Warning => new SolidColorBrush(0xFFF0AD4E),
        _                 => new SolidColorBrush(0xFF2A6DD4),
    };

    internal static string GetIcon(ToastType type) => type switch
    {
        ToastType.Success => "✓",
        ToastType.Error   => "✗",
        ToastType.Warning => "⚠",
        _                 => "ℹ",
    };
}

public enum ToastType { Info, Success, Warning, Error }
