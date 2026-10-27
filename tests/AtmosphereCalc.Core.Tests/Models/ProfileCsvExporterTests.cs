using System.Globalization;
using AtmosphereCalc.Models;

namespace AtmosphereCalc.Core.Tests.Models;

public class ProfileCsvExporterTests
{
    private readonly AtmosphereCalculator _calculator = new();

    private static string[] Lines(string csv) => csv.TrimEnd('\n').Split('\n');

    [Fact]
    public void ToCsv_WithoutParameters_StartsWithHeader()
    {
        var lines = Lines(ProfileCsvExporter.ToCsv(_calculator.BuildProfile(1000, 500)));

        Assert.Equal(ProfileCsvExporter.Header, lines[0]);
        Assert.Equal(1 + 3, lines.Length);
    }

    [Fact]
    public void ToCsv_WithParameters_WritesMetadataLineFirst()
    {
        var profile = _calculator.BuildProfile(1250, 500);
        var parameters = new ProfileParameters(1250, 500, 15);

        var lines = Lines(ProfileCsvExporter.ToCsv(profile, parameters));

        Assert.Equal(
            $"# app=AtmosphereCalc {ProfileCsvExporter.AppVersion}; model=ISA-2layer; " +
            "target_altitude_m=1250; step_m=500; sea_level_temperature_c=15; points=4",
            lines[0]);
        Assert.Equal(ProfileCsvExporter.Header, lines[1]);
        Assert.Equal(2 + 4, lines.Length);
    }

    [Fact]
    public void ToCsv_MetadataLine_HasNoCommas()
    {
        var parameters = new ProfileParameters(1234.5, 100.25, -12.5);

        var lines = Lines(ProfileCsvExporter.ToCsv(_calculator.BuildProfile(1234.5, 100.25, -12.5), parameters));

        Assert.DoesNotContain(",", lines[0]);
        Assert.Contains("target_altitude_m=1234.5", lines[0]);
        Assert.Contains("sea_level_temperature_c=-12.5", lines[0]);
    }

    [Fact]
    public void ToCsv_Rows_UseInvariantNumbersAndLayerName()
    {
        var lines = Lines(ProfileCsvExporter.ToCsv([_calculator.Calculate(0), _calculator.Calculate(15000)]));

        Assert.Equal("0,15.00,288.15,101325.00,1013.25,1.2250,Тропосфера", lines[1]);
        Assert.StartsWith("15000,-56.50,216.65,", lines[2]);
        Assert.EndsWith(",Стратосфера", lines[2]);
    }

    [Fact]
    public void ToCsv_DoesNotDependOnSystemCulture()
    {
        var profile = _calculator.BuildProfile(2000, 500);
        string invariant = ProfileCsvExporter.ToCsv(profile);

        var original = CultureInfo.CurrentCulture;
        try
        {
            // В русской локали дробная часть — запятая: она сломала бы столбцы CSV.
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            Assert.Equal(invariant, ProfileCsvExporter.ToCsv(profile));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ToCsv_UsesUnixLineEndings()
    {
        string csv = ProfileCsvExporter.ToCsv(_calculator.BuildProfile(1000, 500), new ProfileParameters(1000, 500, 15));

        Assert.DoesNotContain("\r", csv);
        Assert.EndsWith("\n", csv);
    }

    [Fact]
    public void ToCsv_EveryRow_HasSameColumnCountAsHeader()
    {
        int columns = ProfileCsvExporter.Header.Split(',').Length;

        var lines = Lines(ProfileCsvExporter.ToCsv(_calculator.BuildProfile(25000, 1000)));

        Assert.All(lines, line => Assert.Equal(columns, line.Split(',').Length));
    }
}
