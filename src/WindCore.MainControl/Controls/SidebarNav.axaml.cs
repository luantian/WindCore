using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using IconPacks.Avalonia.FontAwesome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Animation;
using Avalonia.Animation.Easings;

namespace WindCore.MainControl.Controls;

/// <summary>
/// 侧边栏导航项（叶子节点）
/// </summary>
public class NavItem
{
    public string Label { get; set; } = "";
    public string Key { get; set; } = "";
    public PackIconFontAwesomeKind Kind { get; set; } = PackIconFontAwesomeKind.None;
}

/// <summary>
/// 侧边栏导航分组（一级/二级）
/// </summary>
public class NavSection
{
    public string Label { get; set; } = "";
    public int Level { get; set; } = 1;
    public bool IsLeaf { get; set; } = false;
    public PackIconFontAwesomeKind Icon { get; set; } = PackIconFontAwesomeKind.None;
    public List<NavSection> Sections { get; } = new();
    public List<NavItem> Items { get; } = new();
    public bool IsExpanded { get; set; } = true;
}

public partial class SidebarNav : UserControl
{
    // TDesign 颜色
    private static readonly Color PrimaryColor = Color.Parse("#0052D9");
    private static readonly Color PrimaryLight = Color.Parse("#F0F5FF");
    private static readonly Color TextPrimary = Color.Parse("#1D2129");
    private static readonly Color TextSecondary = Color.Parse("#4E5969");
    private static readonly Color TextMuted = Color.Parse("#86909C");
    private static readonly Color DividerColor = Color.Parse("#E8E8E8");
    private static readonly Color BgHover = Color.Parse("#F7F9FC");
    private static readonly Color IconBgNormal = Color.Parse("#F0F2F5");
    private static readonly Color IconBgActive = Color.Parse("#0052D9");

    private readonly StackPanel _root;
    private readonly Dictionary<string, Button> _buttons = new();
    private readonly List<(Button button, StackPanel container, NavSection section)> _folds = new();
    private readonly Dictionary<string, string> _foldStates = new();

    public static readonly DirectProperty<SidebarNav, List<NavSection>> SectionsProperty =
        AvaloniaProperty.RegisterDirect<SidebarNav, List<NavSection>>(
            nameof(Sections),
            o => o.Sections,
            (o, v) => o.Sections = v);

    private List<NavSection> _sections = new();
    public List<NavSection> Sections
    {
        get => _sections;
        set => SetAndRaise(SectionsProperty, ref _sections, value);
    }

    public static readonly StyledProperty<string> SelectedKeyProperty =
        AvaloniaProperty.Register<SidebarNav, string>(nameof(SelectedKey));

    public string SelectedKey
    {
        get => GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    public event EventHandler<string>? ItemClicked;

    public SidebarNav()
    {
        InitializeComponent();
        _root = this.FindControl<StackPanel>("sidebarRoot")!;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SectionsProperty)
        {
            RebuildTree();
        }
        else if (change.Property == SelectedKeyProperty)
        {
            UpdateHighlight();
        }
    }

    private void RebuildTree()
    {
        _buttons.Clear();
        _folds.Clear();
        _root.Children.Clear();

        foreach (var section in Sections)
        {
            AddSection(section, _root);
        }
    }

    private void AddSection(NavSection section, Panel parent)
    {
        if (section.IsLeaf || (section.Sections.Count == 0 && section.Items.Count == 1))
        {
            foreach (var item in section.Items)
            {
                AddItem(item, parent, section.Level);
            }
            return;
        }

        var foldKey = section.Label + section.Level;
        _foldStates[foldKey] = section.IsExpanded ? "expanded" : "collapsed";

        var container = new StackPanel { Spacing = 2, IsVisible = section.IsExpanded, Margin = new Thickness(0, 2, 0, 2) };

        var btn = BuildFoldButton(section, foldKey, container);

        parent.Children.Add(btn);
        parent.Children.Add(container);
        _folds.Add((btn, container, section));

        foreach (var childSection in section.Sections)
        {
            AddSection(childSection, container);
        }

        foreach (var item in section.Items)
        {
            AddItem(item, container, section.Level + 1);
        }
    }

    private Button BuildFoldButton(NavSection section, string foldKey, StackPanel container)
    {
        var btn = new Button
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(TextPrimary),
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            Padding = section.Level == 1 ? new Thickness(12, 10, 12, 8) : new Thickness(24, 8, 12, 6),
        };

        btn.Click += (_, _) =>
        {
            bool isExpanded = _foldStates[foldKey] == "expanded";
            _foldStates[foldKey] = isExpanded ? "collapsed" : "expanded";
            container.IsVisible = !isExpanded;
            UpdateFoldIcon(btn, section, !isExpanded);
        };

        UpdateFoldIcon(btn, section, section.IsExpanded);
        return btn;
    }

    private void AddItem(NavItem item, Panel parent, int level)
    {
        var btn = new Button
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            Padding = level switch
            {
                1 => new Thickness(12, 10),
                2 => new Thickness(24, 8, 12, 8),
                _ => new Thickness(36, 8, 12, 8),
            },
            Margin = new Thickness(4, 1, 4, 1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(TextSecondary),
        };
        btn.Classes.Add("nav-item");

        var content = BuildItemContent(item, level);
        btn.Content = content;

        btn.Click += (_, _) =>
        {
            SelectedKey = item.Key;
            ItemClicked?.Invoke(this, item.Key);
        };

        // Hover 效果
        btn.PointerEntered += (_, _) =>
        {
            if (!btn.Classes.Contains("active"))
            {
                btn.Background = new SolidColorBrush(BgHover);
            }
        };
        btn.PointerExited += (_, _) =>
        {
            if (!btn.Classes.Contains("active"))
            {
                btn.Background = Brushes.Transparent;
            }
        };

        parent.Children.Add(btn);
        _buttons[item.Key] = btn;
    }

    private Panel BuildItemContent(NavItem item, int level)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // 图标带底色圆形背景
        var iconGrid = new Grid
        {
            Width = 26,
            Height = 26,
        };

        var iconBg = new Avalonia.Controls.Shapes.Ellipse
        {
            Fill = new SolidColorBrush(IconBgNormal),
        };

        var icon = new PackIconFontAwesome
        {
            Kind = item.Kind,
            Foreground = new SolidColorBrush(TextSecondary),
        };

        // 用 ViewBox 让图标自适应圆形大小
        var viewBox = new Viewbox
        {
            Width = 14,
            Height = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = icon,
        };

        iconGrid.Children.Add(iconBg);
        iconGrid.Children.Add(viewBox);

        panel.Children.Add(iconGrid);

        var text = new TextBlock
        {
            Text = item.Label,
            FontSize = level >= 3 ? 12 : 13,
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(text);

        return panel;
    }

    private void UpdateFoldIcon(Button btn, NavSection section, bool isExpanded)
    {
        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
            },
        };

        // 左侧图标 - 带圆形背景
        var iconGrid = new Grid
        {
            Width = 28,
            Height = 28,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var iconBg = new Avalonia.Controls.Shapes.Ellipse
        {
            Fill = new SolidColorBrush(IconBgNormal),
        };
        var leftIcon = new PackIconFontAwesome
        {
            Kind = section.Icon,
            Foreground = section.Level == 1 ? new SolidColorBrush(TextPrimary) : new SolidColorBrush(TextSecondary),
        };
        var foldViewBox = new Viewbox
        {
            Width = 16,
            Height = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = leftIcon,
        };
        iconGrid.Children.Add(iconBg);
        iconGrid.Children.Add(foldViewBox);
        Grid.SetColumn(iconGrid, 0);

        var text = new TextBlock
        {
            Text = section.Label,
            FontSize = section.Level == 1 ? 14 : 13,
            FontWeight = section.Level == 1 ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = section.Level == 1 ? new SolidColorBrush(TextPrimary) : new SolidColorBrush(TextSecondary),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
        };
        Grid.SetColumn(text, 1);

        var arrow = new PackIconFontAwesome
        {
            Kind = isExpanded ? PackIconFontAwesomeKind.ChevronDownSolid : PackIconFontAwesomeKind.ChevronRightSolid,
            Width = 10,
            Height = 10,
            Foreground = new SolidColorBrush(TextMuted),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0),
        };
        Grid.SetColumn(arrow, 2);

        grid.Children.Add(iconGrid);
        grid.Children.Add(text);
        grid.Children.Add(arrow);

        btn.Content = grid;
    }

    private void UpdateHighlight()
    {
        foreach (var kvp in _buttons)
        {
            var btn = kvp.Value;
            if (kvp.Key == SelectedKey)
            {
                btn.Classes.Add("active");
                btn.Background = new SolidColorBrush(PrimaryLight);

                // 更新内容颜色
                if (btn.Content is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is Grid iconGrid && iconGrid.Children.Count >= 2)
                        {
                            // 图标背景改为主色
                            if (iconGrid.Children[0] is Avalonia.Controls.Shapes.Ellipse ellipse)
                            {
                                ellipse.Fill = new SolidColorBrush(IconBgActive);
                            }
                            // Viewbox 中的图标改白
                            if (iconGrid.Children[1] is Viewbox vb && vb.Child is PackIconFontAwesome faIcon)
                            {
                                faIcon.Foreground = Brushes.White;
                            }
                        }
                        if (child is TextBlock tb)
                        {
                            tb.Foreground = new SolidColorBrush(PrimaryColor);
                            tb.FontWeight = FontWeight.SemiBold;
                        }
                    }
                }
            }
            else
            {
                btn.Classes.Remove("active");
                btn.Background = Brushes.Transparent;

                // 恢复默认颜色
                if (btn.Content is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is Grid iconGrid && iconGrid.Children.Count >= 2)
                        {
                            if (iconGrid.Children[0] is Avalonia.Controls.Shapes.Ellipse ellipse)
                            {
                                ellipse.Fill = new SolidColorBrush(IconBgNormal);
                            }
                            if (iconGrid.Children[1] is Viewbox vb && vb.Child is PackIconFontAwesome faIcon)
                            {
                                faIcon.Foreground = new SolidColorBrush(TextSecondary);
                            }
                        }
                        if (child is TextBlock tb)
                        {
                            tb.Foreground = new SolidColorBrush(TextSecondary);
                            tb.FontWeight = FontWeight.Normal;
                        }
                    }
                }
            }
        }
    }
}
