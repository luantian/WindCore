using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WindCore.MainControl.Controls;

public class ConfirmDialog : ContentControl
{
    private Control? _dialogPanel;
    private Action? _onConfirm;
    private Action? _onCancel;

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Title), defaultValue: "确认操作");
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Message));
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ConfirmDialog, bool>(nameof(IsOpen));
    public static readonly StyledProperty<ICommand> ConfirmCommandProperty =
        AvaloniaProperty.Register<ConfirmDialog, ICommand>(nameof(ConfirmCommand));
    public static readonly StyledProperty<ICommand> CancelCommandProperty =
        AvaloniaProperty.Register<ConfirmDialog, ICommand>(nameof(CancelCommand));

    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public ICommand ConfirmCommand { get => GetValue(ConfirmCommandProperty); set => SetValue(ConfirmCommandProperty, value); }
    public ICommand CancelCommand { get => GetValue(CancelCommandProperty); set => SetValue(CancelCommandProperty, value); }

    public ConfirmDialog()
    {
        IsVisible = false;
        ConfirmCommand = new SimpleCommand(DoConfirm);
        CancelCommand = new SimpleCommand(DoCancel);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _dialogPanel = e.NameScope.Find<Control>("dialogPanel");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty)
        {
            IsVisible = change.NewValue is bool b && b;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsOpen) return;
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;

        var source = e.Source as Control;
        if (source != null && _dialogPanel != null && !IsDescendantOf(source, _dialogPanel))
        {
            IsOpen = false;
            _onCancel?.Invoke();
        }
    }

    private static bool IsDescendantOf(Control control, Control ancestor)
    {
        var current = control.Parent as Control;
        while (current != null)
        {
            if (current == ancestor) return true;
            current = current.Parent as Control;
        }
        return false;
    }

    public void Show(string title, string message, Action onConfirm, Action? onCancel = null)
    {
        Title = title;
        Message = message;
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        IsOpen = true;
    }

    private void DoConfirm()
    {
        IsOpen = false;
        _onConfirm?.Invoke();
    }

    private void DoCancel()
    {
        IsOpen = false;
        _onCancel?.Invoke();
    }

    private class SimpleCommand(Action execute) : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
