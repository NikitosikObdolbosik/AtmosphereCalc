using System.Globalization;

namespace AtmosphereCalc.ViewModels;

internal static class Formatting
{
    /// <summary>
    /// Число с точкой в дробной части и пробелом между разрядами (101 325.00),
    /// независимо от языка системы.
    /// </summary>
    public static string Number(double value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture).Replace(",", " ");

    /// <summary>
    /// Разбирает число, принимая и точку, и запятую как разделитель дробной части.
    /// «NaN», «Infinity» и переполнение (например, 1e400) числом не считаются.
    /// </summary>
    public static bool TryParse(string? text, out double value) =>
        double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value);
}
