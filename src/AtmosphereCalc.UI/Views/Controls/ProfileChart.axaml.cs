using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AtmosphereCalc.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AtmosphereCalc.Views.Controls;

/// <summary>
/// График величины по высоте: оси с делениями и единицами, сетка, кривая и маркер текущей точки.
/// Только отрисовка: точки и подписи приходят через привязки, деления считает <see cref="AxisTicks"/>.
/// </summary>
public partial class ProfileChart : UserControl
{
    // Поля вокруг области построения: слева — подписи делений Y, снизу — делений X и название оси.
    private const double MarginLeft = 64;
    private const double MarginRight = 24;
    private const double MarginTop = 36;
    private const double MarginBottom = 48;
    private const double LabelOffset = 15;

    private static readonly IBrush GridBrush = new SolidColorBrush(Color.Parse("#EEF2F7"));
    private static readonly IBrush AxisBrush = new SolidColorBrush(Color.Parse("#CBD5E1"));
    private static readonly IBrush TickTextBrush = new SolidColorBrush(Color.Parse("#94A3B8"));
    private static readonly IBrush TitleBrush = new SolidColorBrush(Color.Parse("#64748B"));

    public static readonly StyledProperty<IReadOnlyList<ChartPoint>?> PointsProperty =
        AvaloniaProperty.Register<ProfileChart, IReadOnlyList<ChartPoint>?>(nameof(Points));

    public static readonly StyledProperty<ChartPoint?> MarkerProperty =
        AvaloniaProperty.Register<ProfileChart, ChartPoint?>(nameof(Marker));

    public static readonly StyledProperty<string?> MarkerTextProperty =
        AvaloniaProperty.Register<ProfileChart, string?>(nameof(MarkerText));

    public static readonly StyledProperty<string?> XAxisTitleProperty =
        AvaloniaProperty.Register<ProfileChart, string?>(nameof(XAxisTitle));

    public static readonly StyledProperty<string?> YAxisTitleProperty =
        AvaloniaProperty.Register<ProfileChart, string?>(nameof(YAxisTitle));

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<ProfileChart, string?>(nameof(Caption));

    public IReadOnlyList<ChartPoint>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public ChartPoint? Marker
    {
        get => GetValue(MarkerProperty);
        set => SetValue(MarkerProperty, value);
    }

    public string? MarkerText
    {
        get => GetValue(MarkerTextProperty);
        set => SetValue(MarkerTextProperty, value);
    }

    /// <summary>Подпись оси X с единицами, например «h, м».</summary>
    public string? XAxisTitle
    {
        get => GetValue(XAxisTitleProperty);
        set => SetValue(XAxisTitleProperty, value);
    }

    /// <summary>Подпись оси Y с единицами, например «P, Па».</summary>
    public string? YAxisTitle
    {
        get => GetValue(YAxisTitleProperty);
        set => SetValue(YAxisTitleProperty, value);
    }

    /// <summary>Параметры расчёта в правом верхнем углу — попадают и в сохранённый PNG.</summary>
    public string? Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public ProfileChart()
    {
        InitializeComponent();
        GraphCanvas.SizeChanged += (_, _) => Redraw();
    }

    /// <summary>Снимок графика в PNG с двойным разрешением, или null, если график ещё не отрисован.</summary>
    public byte[]? CapturePng(double scale = 2)
    {
        var size = Bounds.Size;
        if (size.Width < 1 || size.Height < 1) return null;

        var pixels = new PixelSize((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale));
        using var bitmap = new RenderTargetBitmap(pixels, new Vector(96 * scale, 96 * scale));
        bitmap.Render(this);

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PointsProperty ||
            change.Property == MarkerProperty ||
            change.Property == MarkerTextProperty ||
            change.Property == XAxisTitleProperty ||
            change.Property == YAxisTitleProperty ||
            change.Property == CaptionProperty)
        {
            Redraw();
        }
    }

    private void Redraw()
    {
        AxisLayer.Children.Clear();

        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;
        var points = Points;

        bool hasData = width > MarginLeft + MarginRight && height > MarginTop + MarginBottom && points is { Count: > 0 };
        ValueCurve.IsVisible = ValueArea.IsVisible = hasData;
        if (!hasData)
        {
            CurrentPointMarker.IsVisible = MarkerLabel.IsVisible = false;
            return;
        }

        var plot = new Rect(MarginLeft, MarginTop, width - MarginLeft - MarginRight, height - MarginTop - MarginBottom);
        var xTicks = AxisTicks.For(points!.Min(p => p.X), points!.Max(p => p.X), targetCount: 6);
        var yTicks = AxisTicks.For(points!.Min(p => p.Y), points!.Max(p => p.Y), targetCount: 6);
        var scale = new Scale(xTicks, yTicks, plot);

        DrawAxes(plot, xTicks, yTicks, scale, width);

        var curve = points!.Select(scale.ToCanvas).ToList();
        ValueCurve.Points = curve;
        ValueArea.Points = [.. curve, new Point(curve[^1].X, plot.Bottom), new Point(curve[0].X, plot.Bottom)];

        UpdateMarker(scale, plot);
    }

    private void DrawAxes(Rect plot, AxisTicks xTicks, AxisTicks yTicks, Scale scale, double width)
    {
        foreach (double value in yTicks.Values)
        {
            double y = scale.ToCanvas(new ChartPoint(xTicks.Min, value)).Y;
            AddLine(new Point(plot.Left, y), new Point(plot.Right, y), GridBrush);
            AddText(yTicks.Format(value), plot.Left - 8, y, TickTextBrush, 11, alignX: 1, alignY: 0.5);
        }

        foreach (double value in xTicks.Values)
        {
            double x = scale.ToCanvas(new ChartPoint(value, yTicks.Min)).X;
            AddLine(new Point(x, plot.Bottom), new Point(x, plot.Bottom + 4), AxisBrush);
            AddText(xTicks.Format(value), x, plot.Bottom + 7, TickTextBrush, 11, alignX: 0.5, alignY: 0);
        }

        AddLine(new Point(plot.Left, plot.Top), new Point(plot.Left, plot.Bottom), AxisBrush);
        AddLine(new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom), AxisBrush);

        if (!string.IsNullOrEmpty(YAxisTitle))
            AddText(YAxisTitle, 12, 10, TitleBrush, 12, alignX: 0, alignY: 0, bold: true);
        if (!string.IsNullOrEmpty(XAxisTitle))
            AddText(XAxisTitle, plot.Right, plot.Bottom + 26, TitleBrush, 12, alignX: 1, alignY: 0, bold: true);
        if (!string.IsNullOrEmpty(Caption))
            AddText(Caption, width - MarginRight, 10, TickTextBrush, 12, alignX: 1, alignY: 0);
    }

    private void AddLine(Point from, Point to, IBrush brush) =>
        AxisLayer.Children.Add(new Line { StartPoint = from, EndPoint = to, Stroke = brush, StrokeThickness = 1 });

    // alignX/alignY: 0 — текст начинается в точке, 0.5 — центрирован, 1 — заканчивается в точке.
    private void AddText(string text, double x, double y, IBrush brush, double fontSize,
        double alignX, double alignY, bool bold = false)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = brush,
            FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal
        };
        block.Measure(Size.Infinity);
        Canvas.SetLeft(block, x - block.DesiredSize.Width * alignX);
        Canvas.SetTop(block, y - block.DesiredSize.Height * alignY);
        AxisLayer.Children.Add(block);
    }

    private void UpdateMarker(Scale scale, Rect plot)
    {
        bool visible = Marker is not null;
        CurrentPointMarker.IsVisible = MarkerLabel.IsVisible = visible;
        if (!visible) return;

        var point = scale.ToCanvas(Marker!.Value);
        Canvas.SetLeft(CurrentPointMarker, point.X);
        Canvas.SetTop(CurrentPointMarker, point.Y);

        MarkerLabelText.Text = MarkerText;
        MarkerLabel.Measure(Size.Infinity);
        var label = MarkerLabel.DesiredSize;

        // Подпись справа от точки, а если не помещается — слева; над точкой, если есть место.
        double left = point.X + LabelOffset + label.Width <= plot.Right
            ? point.X + LabelOffset
            : point.X - LabelOffset - label.Width;
        double top = point.Y - LabelOffset - label.Height >= plot.Top
            ? point.Y - LabelOffset - label.Height
            : point.Y + LabelOffset;

        Canvas.SetLeft(MarkerLabel, Math.Max(plot.Left, left));
        Canvas.SetTop(MarkerLabel, top);
    }

    /// <summary>Перевод координат данных в пиксели холста по границам осей.</summary>
    private readonly struct Scale(AxisTicks x, AxisTicks y, Rect plot)
    {
        public Point ToCanvas(ChartPoint p) => new(
            plot.Left + (p.X - x.Min) / (x.Max - x.Min) * plot.Width,
            plot.Bottom - (p.Y - y.Min) / (y.Max - y.Min) * plot.Height);
    }
}
