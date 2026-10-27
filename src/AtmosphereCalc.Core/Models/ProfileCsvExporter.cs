using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AtmosphereCalc.Models;

/// <summary>
/// Превращает профиль атмосферы в CSV: разделитель — запятая, дробная часть — точка,
/// чтобы файл одинаково читался на любой локали.
/// Первая строка (начинается с <c>#</c>) хранит параметры расчёта и версию программы.
/// </summary>
public static class ProfileCsvExporter
{
    public const string Header =
        "altitude_m,temperature_c,temperature_k,pressure_pa,pressure_hpa,density_kg_m3,layer";

    public const string ModelName = "ISA-2layer";

    public static string AppVersion { get; } =
        typeof(ProfileCsvExporter).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static string ToCsv(IEnumerable<AtmosphereData> profile, ProfileParameters? parameters = null)
    {
        var culture = CultureInfo.InvariantCulture;
        var rows = profile.ToList();
        var csv = new StringBuilder();

        if (parameters is not null)
            csv.Append(MetadataLine(parameters, rows.Count)).Append('\n');

        csv.Append(Header).Append('\n');

        foreach (var row in rows)
        {
            csv.Append(string.Join(',',
                row.Altitude.ToString("0.##", culture),
                row.TemperatureC.ToString("0.00", culture),
                row.TemperatureK.ToString("0.00", culture),
                row.Pressure.ToString("0.00", culture),
                row.PressureHpa.ToString("0.00", culture),
                row.Density.ToString("0.0000", culture),
                row.Layer.DisplayName()));
            csv.Append('\n');
        }

        return csv.ToString();
    }

    // Без запятых, чтобы строка не разбивалась на столбцы при открытии в Excel.
    private static string MetadataLine(ProfileParameters p, int points)
    {
        var culture = CultureInfo.InvariantCulture;
        return "# " + string.Join("; ",
            $"app=AtmosphereCalc {AppVersion}",
            $"model={ModelName}",
            $"target_altitude_m={p.TargetAltitude.ToString("0.##", culture)}",
            $"step_m={p.Step.ToString("0.##", culture)}",
            $"sea_level_temperature_c={p.SeaLevelTemperatureC.ToString("0.##", culture)}",
            $"points={points}");
    }
}
