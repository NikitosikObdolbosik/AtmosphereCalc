using System;
using System.Collections.Generic;

namespace AtmosphereCalc.ViewModels;

/// <summary>
/// Деления оси с «круглым» шагом (1, 2 или 5 × 10ⁿ), охватывающие диапазон данных.
/// Чистая математика без UI, поэтому покрыта тестами.
/// </summary>
public sealed record AxisTicks(double Min, double Max, double Step, IReadOnlyList<double> Values)
{
    public static AxisTicks For(double dataMin, double dataMax, int targetCount = 5)
    {
        if (!double.IsFinite(dataMin) || !double.IsFinite(dataMax))
            throw new ArgumentException("Границы оси должны быть конечными числами");

        if (dataMax < dataMin)
            (dataMin, dataMax) = (dataMax, dataMin);

        // Постоянная величина (например, одна точка): раздвигаем диапазон, чтобы было что делить.
        if (dataMax - dataMin < 1e-12)
        {
            double pad = Math.Abs(dataMin) > 1e-12 ? Math.Abs(dataMin) * 0.1 : 1;
            dataMin -= pad;
            dataMax += pad;
        }

        double step = NiceStep((dataMax - dataMin) / Math.Max(1, targetCount - 1));
        double min = Math.Floor(dataMin / step + 1e-9) * step;
        double max = Math.Ceiling(dataMax / step - 1e-9) * step;

        var values = new List<double>();
        for (double v = min; v <= max + step * 1e-6; v += step)
            values.Add(Math.Round(v / step) * step);

        return new AxisTicks(min, max, step, values);
    }

    /// <summary>Подпись деления: столько знаков после точки, сколько нужно шагу.</summary>
    public string Format(double value)
    {
        int decimals = Math.Max(0, (int)-Math.Floor(Math.Log10(Step) + 1e-9));
        string format = "#,0" + (decimals > 0 ? "." + new string('0', decimals) : "");
        // Избегаем «-0» у нулевого деления.
        return Formatting.Number(Math.Abs(value) < Step * 1e-9 ? 0 : value, format);
    }

    private static double NiceStep(double rough)
    {
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        double fraction = rough / magnitude;
        double nice = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
        return nice * magnitude;
    }
}
