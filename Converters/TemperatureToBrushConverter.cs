using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AtmosphereCalc.Converters;

/// <summary>
/// Превращает температуру (°C) в диагональный градиент: от «холодных» цветов к «тёплым».
/// </summary>
public sealed class TemperatureToBrushConverter : IValueConverter
{
    public double MinTemperature { get; set; } = -56.5;
    public double MaxTemperature { get; set; } = 15.0;

    public Color ColdStart { get; set; }
    public Color ColdEnd { get; set; }
    public Color HotStart { get; set; }
    public Color HotEnd { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double celsius) return null;

        double clamped = Math.Clamp(celsius, MinTemperature, MaxTemperature);
        double t = (clamped - MinTemperature) / (MaxTemperature - MinTemperature);

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Interpolate(ColdStart, HotStart, t), 0),
                new GradientStop(Interpolate(ColdEnd, HotEnd, t), 1)
            }
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color Interpolate(Color c1, Color c2, double t) => Color.FromRgb(
        (byte)(c1.R + (c2.R - c1.R) * t),
        (byte)(c1.G + (c2.G - c1.G) * t),
        (byte)(c1.B + (c2.B - c1.B) * t));
}
