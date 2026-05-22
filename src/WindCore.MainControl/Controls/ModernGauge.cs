using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace WindCore.MainControl.Controls;

/// <summary>
/// 高精度仪表盘控件 — 汽车级品质
/// </summary>
public class ModernGauge : Control
{
    // Drawing layer
    private readonly Canvas _drawCanvas;
    private readonly Path _glowArc;         // 发光弧（粗、半透明）
    private readonly Path _bgArc;           // 背景轨道（浅灰细线）
    private readonly Path _bgTrack;         // 背景轨道（深色底条）
    private readonly Path _valueArc;        // 值弧（彩色粗线）
    private readonly Polygon _needle;       // 锥形指针
    private readonly Polygon _needleTail;   // 指针尾部配重
    private readonly Ellipse _centerRing;   // 中心金属环
    private readonly Ellipse _centerDot;    // 中心圆点

    // Tick marks pool
    private readonly Line[] _majorTicks = new Line[10];
    private readonly Line[] _minorTicks = new Line[40];

    // Text elements
    private readonly TextBlock _valueText;
    private readonly TextBlock _unitText;
    private readonly TextBlock _minText;
    private readonly TextBlock _maxText;
    private readonly TextBlock _titleLabel;

    // Reusable colors (avoid allocation)
    private readonly SolidColorBrush _glowBrush;
    private readonly SolidColorBrush _valueBrush;
    private readonly SolidColorBrush _tickBrush;
    private readonly SolidColorBrush _majorTickBrush;

    private bool _initialized;
    private Color _currentColor;

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ModernGauge, double>(nameof(Value), defaultValue: 0);
    public static readonly StyledProperty<double> MinProperty =
        AvaloniaProperty.Register<ModernGauge, double>(nameof(Min), defaultValue: 0);
    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<ModernGauge, double>(nameof(Max), defaultValue: 100);
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ModernGauge, string>(nameof(Title), defaultValue: "");
    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<ModernGauge, string>(nameof(Unit), defaultValue: "");
    public static readonly StyledProperty<double> WarningThresholdProperty =
        AvaloniaProperty.Register<ModernGauge, double>(nameof(WarningThreshold), defaultValue: double.NaN);
    public static readonly StyledProperty<double> DangerThresholdProperty =
        AvaloniaProperty.Register<ModernGauge, double>(nameof(DangerThreshold), defaultValue: double.NaN);

    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Min { get => GetValue(MinProperty); set => SetValue(MinProperty, value); }
    public double Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public double WarningThreshold { get => GetValue(WarningThresholdProperty); set => SetValue(WarningThresholdProperty, value); }
    public double DangerThreshold { get => GetValue(DangerThresholdProperty); set => SetValue(DangerThresholdProperty, value); }

    public ModernGauge()
    {
        ClipToBounds = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        var primary = Color.Parse("#0052D9");
        var warning = Color.Parse("#ED7B2F");
        var danger = Color.Parse("#E34D59");

        _currentColor = primary;

        _drawCanvas = new Canvas();

        // Glow arc (behind value arc, thicker, semi-transparent)
        _glowArc = new Path
        {
            StrokeThickness = 24,
            StrokeLineCap = PenLineCap.Round,
            IsVisible = false,
        };
        _glowBrush = new SolidColorBrush(primary, 0.12);
        _glowArc.Stroke = _glowBrush;

        // Background track (thin subtle line)
        _bgTrack = new Path
        {
            Stroke = new SolidColorBrush(Color.Parse("#E8E8E8")),
            StrokeThickness = 20,
            StrokeLineCap = PenLineCap.Round,
        };

        // Background arc (lighter overlay)
        _bgArc = new Path
        {
            Stroke = new SolidColorBrush(Color.Parse("#F8F9FA")),
            StrokeThickness = 18,
            StrokeLineCap = PenLineCap.Round,
        };

        // Value arc
        _valueArc = new Path
        {
            StrokeThickness = 14,
            StrokeLineCap = PenLineCap.Round,
        };
        _valueBrush = new SolidColorBrush(primary);
        _valueArc.Stroke = _valueBrush;

        // Tapered needle (polygon)
        _needle = new Polygon
        {
            Fill = new SolidColorBrush(Color.Parse("#1D2129")),
            Stroke = null,
        };

        // Needle tail counterweight
        _needleTail = new Polygon
        {
            Fill = new SolidColorBrush(Color.Parse("#C9CDD4")),
            Stroke = null,
        };

        // Center ring
        _centerRing = new Ellipse
        {
            Width = 22, Height = 22,
            Fill = new SolidColorBrush(Color.Parse("#F0F1F3")),
            Stroke = new SolidColorBrush(Color.Parse("#C9CDD4")),
            StrokeThickness = 1.5,
        };
        // Center dot
        _centerDot = new Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(Color.Parse("#1D2129")),
        };

        // Pre-create major ticks (10) + minor ticks (40)
        _tickBrush = new SolidColorBrush(Color.Parse("#C9CDD4"));
        _majorTickBrush = new SolidColorBrush(Color.Parse("#86909C"));
        for (int i = 0; i < 10; i++)
        {
            _majorTicks[i] = new Line { Stroke = _majorTickBrush, StrokeThickness = 2 };
            _drawCanvas.Children.Add(_majorTicks[i]);
        }
        for (int i = 0; i < 40; i++)
        {
            _minorTicks[i] = new Line { Stroke = _tickBrush, StrokeThickness = 1 };
            _drawCanvas.Children.Add(_minorTicks[i]);
        }

        // Drawing layer order (back to front)
        _drawCanvas.Children.Add(_bgTrack);
        _drawCanvas.Children.Add(_bgArc);
        _drawCanvas.Children.Add(_glowArc);
        _drawCanvas.Children.Add(_valueArc);
        _drawCanvas.Children.Add(_needle);
        _drawCanvas.Children.Add(_needleTail);
        _drawCanvas.Children.Add(_centerRing);
        _drawCanvas.Children.Add(_centerDot);

        // Text overlays
        _titleLabel = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#1D2129")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 0, 0),
        };
        _valueText = new TextBlock
        {
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#1D2129")),
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _unitText = new TextBlock
        {
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#86909C")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, -2, 0, 0),
        };
        _minText = new TextBlock
        {
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.Parse("#86909C")),
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        _maxText = new TextBlock
        {
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.Parse("#86909C")),
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        // Layout
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(new GridLength(1, GridUnitType.Star)),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            }
        };

        // Top panel: only the drawing canvas (no value text overlay)
        var topPanel = new Panel();
        topPanel.Children.Add(_drawCanvas);

        mainGrid.Children.Add(topPanel);
        Grid.SetRow(topPanel, 0);

        // Bottom panel: value + unit centered below the gauge
        var bottomStack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 0,
            Margin = new Thickness(0, 8, 0, 4),
        };
        bottomStack.Children.Add(_valueText);
        bottomStack.Children.Add(_unitText);
        mainGrid.Children.Add(bottomStack);
        Grid.SetRow(bottomStack, 1);

        var bottomPanel = new Panel { Margin = new Thickness(12, 0, 12, 6) };
        var minMaxGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        minMaxGrid.Children.Add(_minText);
        Grid.SetColumn(_minText, 0);
        minMaxGrid.Children.Add(_maxText);
        Grid.SetColumn(_maxText, 1);
        bottomPanel.Children.Add(minMaxGrid);
        mainGrid.Children.Add(bottomPanel);
        Grid.SetRow(bottomPanel, 2);

        VisualChildren.Add(mainGrid);
        LogicalChildren.Add(mainGrid);

        _initialized = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_initialized) return;
        if (change.Property == ValueProperty || change.Property == WarningThresholdProperty || change.Property == DangerThresholdProperty)
            UpdateGeometry(Bounds.Size);
        else if (change.Property == MinProperty || change.Property == MaxProperty)
            UpdateGeometry(Bounds.Size);
        else if (change.Property == TitleProperty)
            _titleLabel.Text = Title;
        else if (change.Property == UnitProperty)
            _unitText.Text = Unit;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (VisualChildren.Count > 0)
            ((Control)VisualChildren[0]).Measure(availableSize);
        return new Size(220, 200);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (VisualChildren.Count > 0)
            ((Control)VisualChildren[0]).Arrange(new Rect(finalSize));
        UpdateGeometry(finalSize);
        return finalSize;
    }

    private void UpdateGeometry(Size size)
    {
        var width = size.Width;
        var height = size.Height;
        if (width <= 10 || height <= 10) return;

        var radius = Math.Min(width / 2 - 16, height * 0.62);
        var center = new Point(width / 2, height * 0.52);

        // Background arcs
        _bgTrack.Data = CreateArcGeometry(center, radius, 180, 360);
        _bgArc.Data = CreateArcGeometry(center, radius, 180, 360);

        // Value arc + glow
        var range = Math.Max(Max - Min, 0.001);
        var ratio = Math.Clamp((Value - Min) / range, 0, 1);
        var sweepAngle = ratio * 180;
        var color = GetColor(ratio);

        // Update color only if changed (avoid redundant brush creation)
        if (_currentColor != color)
        {
            _currentColor = color;
            _valueBrush.Color = color;
            _glowBrush.Color = Color.FromArgb(30, color.R, color.G, color.B);
        }

        if (sweepAngle > 0.5)
        {
            var arcData = CreateArcGeometry(center, radius, 180, 180 + sweepAngle);
            _valueArc.Data = arcData;
            _valueArc.IsVisible = true;
            _glowArc.Data = arcData;
            _glowArc.IsVisible = true;
        }
        else
        {
            _valueArc.IsVisible = false;
            _glowArc.IsVisible = false;
        }

        // --- Tapered needle ---
        var needleAngle = (180 + sweepAngle) * Math.PI / 180;
        var needleLen = radius - 28;
        var tailLen = 14;
        var nx = center.X + needleLen * Math.Cos(needleAngle);
        var ny = center.Y + needleLen * Math.Sin(needleAngle);
        var tx = center.X - tailLen * Math.Cos(needleAngle);
        var ty = center.Y - tailLen * Math.Sin(needleAngle);

        // Needle width (tapered: wide at center, narrow at tip)
        var perpX = -Math.Sin(needleAngle);
        var perpY = Math.Cos(needleAngle);
        var tipW = 1.5;
        var baseW = 5;
        var tailW = 3;

        _needle.Points = new Point[]
        {
            new Point(nx + perpX * tipW, ny + perpY * tipW),
            new Point(center.X + perpX * baseW, center.Y + perpY * baseW),
            new Point(center.X - perpX * baseW, center.Y - perpY * baseW),
            new Point(nx - perpX * tipW, ny - perpY * tipW),
        };

        _needleTail.Points = new Point[]
        {
            new Point(center.X + perpX * baseW, center.Y + perpY * baseW),
            new Point(tx + perpX * tailW, ty + perpY * tailW),
            new Point(tx - perpX * tailW, ty - perpY * tailW),
            new Point(center.X - perpX * baseW, center.Y - perpY * baseW),
        };

        // Center elements
        Canvas.SetLeft(_centerRing, center.X - 11);
        Canvas.SetTop(_centerRing, center.Y - 11);
        Canvas.SetLeft(_centerDot, center.X - 4);
        Canvas.SetTop(_centerDot, center.Y - 4);

        // --- Tick marks ---
        // 10 major ticks every 18 degrees
        for (int i = 0; i < 10; i++)
        {
            var angle = 180 + i * 18.0;
            var innerR = radius - 22;
            var outerR = radius - 6;
            var rad = angle * Math.PI / 180;
            var line = _majorTicks[i];
            line.StartPoint = new Point(center.X + innerR * Math.Cos(rad), center.Y + innerR * Math.Sin(rad));
            line.EndPoint = new Point(center.X + outerR * Math.Cos(rad), center.Y + outerR * Math.Sin(rad));
            line.IsVisible = true;
        }

        // 40 minor ticks (4 between each major pair)
        for (int i = 0; i < 40; i++)
        {
            var angle = 180 + (i + 0.5) * 3.6;  // 3.6° spacing = 18°/5
            var innerR = radius - 14;
            var outerR = radius - 8;
            var rad = angle * Math.PI / 180;
            var line = _minorTicks[i];
            line.StartPoint = new Point(center.X + innerR * Math.Cos(rad), center.Y + innerR * Math.Sin(rad));
            line.EndPoint = new Point(center.X + outerR * Math.Cos(rad), center.Y + outerR * Math.Sin(rad));
            line.IsVisible = true;
        }

        // Text
        _valueText.Text = Value == Math.Floor(Value) ? $"{Value:F0}" : $"{Value:F1}";
        _valueText.FontSize = Math.Max(18, Math.Min(28, radius * 0.28));
        _unitText.Text = Unit;
        _titleLabel.Text = Title;
        _minText.Text = Min.ToString("F0");
        _maxText.Text = Max.ToString("F0");
    }

    private Geometry CreateArcGeometry(Point center, double radius, double startAngle, double endAngle)
    {
        var startRad = startAngle * Math.PI / 180;
        var endRad = endAngle * Math.PI / 180;
        var startPoint = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
        var endPoint = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));

        return new PathGeometry
        {
            Figures = new PathFigures
            {
                new PathFigure
                {
                    StartPoint = startPoint,
                    Segments = new PathSegments
                    {
                        new ArcSegment
                        {
                            Point = endPoint,
                            Size = new Size(radius, radius),
                            SweepDirection = SweepDirection.Clockwise,
                            IsLargeArc = endAngle - startAngle > 180,
                        }
                    },
                    IsClosed = false,
                }
            }
        };
    }

    private Color GetColor(double ratio)
    {
        if (!double.IsNaN(DangerThreshold) && Value >= DangerThreshold)
            return Color.Parse("#E34D59");
        if (!double.IsNaN(WarningThreshold) && Value >= WarningThreshold)
            return Color.Parse("#ED7B2F");
        if (ratio > 0.85)
            return Color.Parse("#E34D59");
        if (ratio > 0.7)
            return Color.Parse("#ED7B2F");
        return Color.Parse("#0052D9");
    }
}
