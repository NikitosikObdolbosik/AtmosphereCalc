using System.Collections.Generic;
using AtmosphereCalc.Models;
using Avalonia;
using Avalonia.Controls;

namespace AtmosphereCalc.Views.Controls;

/// <summary>
/// График давления по высоте. Только отрисовка: все данные приходят через привязки.
/// </summary>
public partial class PressureChart : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<AtmosphereData>?> ProfileProperty =
        AvaloniaProperty.Register<PressureChart, IReadOnlyList<AtmosphereData>?>(nameof(Profile));

    public static readonly StyledProperty<AtmosphereData?> CurrentProperty =
        AvaloniaProperty.Register<PressureChart, AtmosphereData?>(nameof(Current));

    public static readonly StyledProperty<bool> IsMarkerVisibleProperty =
        AvaloniaProperty.Register<PressureChart, bool>(nameof(IsMarkerVisible), true);

    public static readonly StyledProperty<double> MaxAltitudeProperty =
        AvaloniaProperty.Register<PressureChart, double>(nameof(MaxAltitude), 1);

    public static readonly StyledProperty<double> MaxPressureProperty =
        AvaloniaProperty.Register<PressureChart, double>(nameof(MaxPressure), 1);

    public IReadOnlyList<AtmosphereData>? Profile
    {
        get => GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    public AtmosphereData? Current
    {
        get => GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public bool IsMarkerVisible
    {
        get => GetValue(IsMarkerVisibleProperty);
        set => SetValue(IsMarkerVisibleProperty, value);
    }

    public double MaxAltitude
    {
        get => GetValue(MaxAltitudeProperty);
        set => SetValue(MaxAltitudeProperty, value);
    }

    public double MaxPressure
    {
        get => GetValue(MaxPressureProperty);
        set => SetValue(MaxPressureProperty, value);
    }

    public PressureChart()
    {
        InitializeComponent();
        GraphCanvas.SizeChanged += (_, _) => Redraw();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ProfileProperty ||
            change.Property == MaxAltitudeProperty ||
            change.Property == MaxPressureProperty)
        {
            Redraw();
        }
        else if (change.Property == CurrentProperty || change.Property == IsMarkerVisibleProperty)
        {
            UpdateMarker();
        }
    }

    private void Redraw()
    {
        DrawCurve();
        UpdateMarker();
    }

    private void DrawCurve()
    {
        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;
        if (width == 0 || height == 0 || Profile is null) return;

        var points = new List<Point>(Profile.Count + 2);
        foreach (var data in Profile)
            points.Add(ToCanvas(data, width, height));

        PressureCurve.Points = points;

        var fillPoints = new List<Point>(points)
        {
            new(width, height),
            new(0, height)
        };
        PressureArea.Points = fillPoints;
    }

    private void UpdateMarker()
    {
        bool visible = IsMarkerVisible && Current is not null;
        CurrentPointMarker.IsVisible = visible;
        MarkerLabel.IsVisible = visible;

        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;
        if (!visible || width == 0) return;

        var point = ToCanvas(Current!, width, height);

        Canvas.SetLeft(CurrentPointMarker, point.X);
        Canvas.SetTop(CurrentPointMarker, point.Y);
        Canvas.SetLeft(MarkerLabel, point.X);
        Canvas.SetTop(MarkerLabel, point.Y);
        MarkerText.Text = $"{Current!.Altitude:F0} м\n{Current.Pressure:F0} Па";
    }

    private Point ToCanvas(AtmosphereData data, double width, double height) => new(
        data.Altitude / MaxAltitude * width,
        height - data.Pressure / MaxPressure * height);
}
