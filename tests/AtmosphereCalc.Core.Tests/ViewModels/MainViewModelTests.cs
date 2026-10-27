using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtmosphereCalc.Models;
using AtmosphereCalc.Services;
using AtmosphereCalc.ViewModels;

namespace AtmosphereCalc.Core.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly FakeFileSaveService _files = new();
    private readonly FakeChartSnapshot _chart = new();
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _vm = new MainViewModel(_files, _chart);
    }

    // ---------- Начальное состояние ----------

    [Fact]
    public void Constructor_CalculatesDefaultProfile()
    {
        Assert.Equal("10000", _vm.AltitudeText);
        Assert.Equal("500", _vm.StepText);
        Assert.Equal("15", _vm.SeaLevelTemperatureText);
        Assert.Equal(10000, _vm.Altitude);

        Assert.Equal(21, _vm.Profile.Count);
        Assert.Equal(10000, _vm.Current!.Altitude);
        Assert.False(_vm.IsResultOutdated);
        Assert.True(_vm.CalculateCommand.CanExecute(null));
    }

    [Fact]
    public void Constructor_FormatsCardTexts()
    {
        Assert.Equal("На высоте 10 000 м", _vm.CurrentAltitudeText);
        Assert.Equal("-50.00 °C", _vm.TemperatureText);
        Assert.Equal("223.15 K", _vm.TemperatureKText);
        Assert.Equal("26 436.87 Па", _vm.PressureText);
        Assert.Equal("264.37 гПа", _vm.PressureHpaText);
        Assert.Equal("0.4127 кг/м³", _vm.DensityText);
        Assert.Equal("Тропосфера", _vm.LayerText);
        Assert.Equal("У земли 15 °C · шаг 500 м", _vm.ChartCaption);
    }

    // ---------- Расчёт по кнопке ----------

    [Fact]
    public void ChangingInput_MarksResultOutdated_WithoutRecalculating()
    {
        _vm.AltitudeText = "5000";

        Assert.True(_vm.IsResultOutdated);
        Assert.Equal(10000, _vm.Current!.Altitude);
    }

    [Fact]
    public void Calculate_UsesNewInput_AndClearsOutdatedFlag()
    {
        _vm.AltitudeText = "5000";
        _vm.CalculateCommand.Execute(null);

        Assert.False(_vm.IsResultOutdated);
        Assert.Equal(5000, _vm.Current!.Altitude);
        Assert.Equal("-17.50 °C", _vm.TemperatureText);
        Assert.Equal(11, _vm.Profile.Count);
    }

    [Fact]
    public void Calculate_Stratosphere_ShowsLayerAndConstantTemperature()
    {
        _vm.AltitudeText = "15000";
        _vm.CalculateCommand.Execute(null);

        Assert.Equal("Стратосфера", _vm.LayerText);
        Assert.Equal("-56.50 °C", _vm.TemperatureText);
    }

    [Fact]
    public void Calculate_CustomSeaLevelTemperature_UpdatesCaptionAndValues()
    {
        _vm.SeaLevelTemperatureText = "-10";
        _vm.StepText = "1000";
        _vm.CalculateCommand.Execute(null);

        Assert.Equal("У земли -10 °C · шаг 1 000 м", _vm.ChartCaption);
        Assert.Equal(-10.0, _vm.Profile[0].TemperatureC, 2);
        Assert.Equal(11, _vm.Profile.Count);
    }

    [Fact]
    public void Calculate_UnevenStep_EndsAtTargetAltitude()
    {
        _vm.AltitudeText = "1250";
        _vm.CalculateCommand.Execute(null);

        Assert.Equal(new double[] { 0, 500, 1000, 1250 }, _vm.Profile.Select(p => p.Altitude));
    }

    [Fact]
    public void Calculate_FillsTableRows()
    {
        Assert.Equal(_vm.Profile.Count, _vm.ProfileRows.Count);

        var first = _vm.ProfileRows[0];
        Assert.Equal("0", first.Altitude);
        Assert.Equal("15.00", first.TemperatureC);
        Assert.Equal("101 325.00", first.Pressure);
        Assert.Equal("1013.25", first.PressureHpa);
        Assert.Equal("1.2250", first.Density);
        Assert.Equal("Тропосфера", first.Layer);
        Assert.Equal("10 000", _vm.ProfileRows[^1].Altitude);
    }

    // ---------- Проверка ввода ----------

    [Theory]
    [InlineData("", MainViewModel.EmptyFieldError)]
    [InlineData("   ", MainViewModel.EmptyFieldError)]
    [InlineData("abc", MainViewModel.NotANumberError)]
    [InlineData("12a", MainViewModel.NotANumberError)]
    [InlineData("NaN", MainViewModel.NotANumberError)]
    [InlineData("1e400", MainViewModel.NotANumberError)]
    [InlineData("-1", "Высота не может быть отрицательной")]
    public void AltitudeText_Invalid_ShowsErrorAndBlocksCalculate(string text, string expectedError)
    {
        _vm.AltitudeText = text;

        Assert.Equal(expectedError, _vm.AltitudeError);
        Assert.True(_vm.HasAltitudeError);
        Assert.False(_vm.CalculateCommand.CanExecute(null));
    }

    [Fact]
    public void AltitudeText_AboveModel_ShowsLimit()
    {
        _vm.AltitudeText = "25001";

        Assert.Contains("25 000", _vm.AltitudeError);
    }

    [Fact]
    public void AltitudeText_WithComma_IsParsed()
    {
        _vm.AltitudeText = "1500,5";

        Assert.Null(_vm.AltitudeError);
        Assert.Equal(1500.5, _vm.Altitude);
    }

    [Fact]
    public void AltitudeText_ValidAfterError_ClearsErrorAndUnblocksCalculate()
    {
        _vm.AltitudeText = "abc";
        _vm.AltitudeText = "3000";

        Assert.Null(_vm.AltitudeError);
        Assert.False(_vm.HasAltitudeError);
        Assert.True(_vm.CalculateCommand.CanExecute(null));
    }

    [Fact]
    public void AltitudeText_Invalid_KeepsLastResultAndSlider()
    {
        var before = _vm.Current;

        _vm.AltitudeText = "abc";

        Assert.Same(before, _vm.Current);
        Assert.Equal(10000, _vm.Altitude);
    }

    [Theory]
    [InlineData("50")]
    [InlineData("20000")]
    [InlineData("abc")]
    public void StepText_Invalid_ShowsErrorAndBlocksCalculate(string text)
    {
        _vm.StepText = text;

        Assert.NotNull(_vm.StepError);
        Assert.True(_vm.HasStepError);
        Assert.False(_vm.CalculateCommand.CanExecute(null));
    }

    [Fact]
    public void StepText_IsRecheckedWhenAltitudeChanges()
    {
        _vm.StepText = "800";
        Assert.Null(_vm.StepError);

        _vm.AltitudeText = "500";

        Assert.NotNull(_vm.StepError);
    }

    [Fact]
    public void StepText_NotComparedWithInvalidAltitude()
    {
        _vm.AltitudeText = "abc";

        Assert.Null(_vm.StepError);
    }

    [Theory]
    [InlineData("-50", true)]
    [InlineData("50", true)]
    [InlineData("-50.1", false)]
    [InlineData("60", false)]
    [InlineData("", false)]
    public void SeaLevelTemperatureText_ChecksRange(string text, bool isValid)
    {
        _vm.SeaLevelTemperatureText = text;

        Assert.Equal(isValid, _vm.SeaLevelTemperatureError is null);
        Assert.Equal(!isValid, _vm.HasSeaLevelTemperatureError);
        Assert.Equal(isValid, _vm.CalculateCommand.CanExecute(null));
    }

    [Fact]
    public void Error_RaisesPropertyChangedForHasError()
    {
        var changed = new List<string?>();
        _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _vm.AltitudeText = "abc";

        Assert.Contains(nameof(MainViewModel.AltitudeError), changed);
        Assert.Contains(nameof(MainViewModel.HasAltitudeError), changed);
    }

    // ---------- Слайдер ----------

    [Fact]
    public void AltitudeText_Valid_MovesSlider()
    {
        _vm.AltitudeText = "5000";

        Assert.Equal(5000, _vm.Altitude);
    }

    [Fact]
    public void Altitude_FromSlider_RoundsIntoText()
    {
        _vm.Altitude = 1234.4;

        Assert.Equal("1234", _vm.AltitudeText);
        Assert.Null(_vm.AltitudeError);
        Assert.True(_vm.IsResultOutdated);
    }

    [Fact]
    public void Altitude_FromSlider_ClearsTextError()
    {
        _vm.AltitudeText = "abc";

        _vm.Altitude = 2000;

        Assert.Equal("2000", _vm.AltitudeText);
        Assert.Null(_vm.AltitudeError);
    }

    // ---------- График ----------

    [Fact]
    public void Chart_DefaultsToPressure()
    {
        Assert.Same(ChartOption.All[0], _vm.SelectedChartOption);
        Assert.Equal("P, Па", _vm.ChartYAxisTitle);
        Assert.Equal("atmosphere_pressure.png", _vm.ChartFileName);
        Assert.Equal(_vm.Profile.Count, _vm.ChartPoints.Count);
        Assert.Equal(101325.0, _vm.ChartPoints[0].Y, 2);
        Assert.Equal(10000, _vm.ChartMarker!.Value.X);
    }

    [Fact]
    public void Chart_SelectingTemperature_SwitchesSeriesAndAxis()
    {
        var changed = new List<string?>();
        _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _vm.SelectedChartOption = ChartOption.All[2];

        Assert.Equal("T, °C", _vm.ChartYAxisTitle);
        Assert.Equal("atmosphere_temperature.png", _vm.ChartFileName);
        Assert.Equal(15.0, _vm.ChartPoints[0].Y, 2);
        Assert.Equal(-50.0, _vm.ChartMarker!.Value.Y, 2);
        Assert.Equal("10 000 м\n-50.00 °C", _vm.ChartMarkerText);
        Assert.Contains(nameof(MainViewModel.ChartPoints), changed);
        Assert.Contains(nameof(MainViewModel.ChartYAxisTitle), changed);
    }

    // ---------- Экспорт ----------

    [Fact]
    public async Task ExportCsv_SavesProfileWithParameters()
    {
        await _vm.ExportCsvCommand.ExecuteAsync(null);

        Assert.Equal(MainViewModel.ExportFileName, _files.LastFileName);
        Assert.StartsWith("# app=AtmosphereCalc", _files.LastText);
        Assert.Contains("target_altitude_m=10000; step_m=500; sea_level_temperature_c=15; points=21", _files.LastText);
        Assert.Equal($"Сохранено: {MainViewModel.ExportFileName}", _vm.ExportStatus);
    }

    [Fact]
    public async Task ExportCsv_DescribesShownResult_NotPendingInput()
    {
        _vm.AltitudeText = "5000";

        await _vm.ExportCsvCommand.ExecuteAsync(null);

        Assert.Contains("target_altitude_m=10000", _files.LastText);
    }

    [Fact]
    public async Task ExportCsv_Cancelled_ShowsNoStatus()
    {
        _files.Cancel = true;

        await _vm.ExportCsvCommand.ExecuteAsync(null);

        Assert.Null(_vm.ExportStatus);
    }

    [Fact]
    public async Task ExportCsv_Failure_ShowsErrorInsteadOfCrashing()
    {
        _files.Error = new UnauthorizedAccessException("нет доступа");

        await _vm.ExportCsvCommand.ExecuteAsync(null);

        Assert.Equal("Не удалось сохранить файл: нет доступа", _vm.ExportStatus);
    }

    [Fact]
    public void Calculate_ClearsExportStatus()
    {
        // Подмена сохраняет синхронно, поэтому статус готов сразу после Execute.
        _vm.ExportCsvCommand.Execute(null);
        Assert.NotNull(_vm.ExportStatus);

        _vm.CalculateCommand.Execute(null);

        Assert.Null(_vm.ExportStatus);
    }

    [Fact]
    public async Task ExportPng_SavesSnapshotUnderChartFileName()
    {
        _vm.SelectedChartOption = ChartOption.All[1];

        await _vm.ExportPngCommand.ExecuteAsync(null);

        Assert.Equal("atmosphere_density.png", _files.LastFileName);
        Assert.Equal(_chart.Png, _files.LastBytes);
    }

    [Fact]
    public async Task ExportPng_ChartNotRendered_ShowsError()
    {
        _chart.Png = null;

        await _vm.ExportPngCommand.ExecuteAsync(null);

        Assert.Null(_files.LastFileName);
        Assert.Contains("график ещё не отрисован", _vm.ExportStatus);
    }

    [Fact]
    public void Export_WithoutServices_IsDisabled()
    {
        var designer = new MainViewModel();

        Assert.False(designer.ExportCsvCommand.CanExecute(null));
        Assert.False(designer.ExportPngCommand.CanExecute(null));
    }

    [Fact]
    public void ExportPng_WithoutSnapshotProvider_IsDisabled()
    {
        var vm = new MainViewModel(new AtmosphereCalculator(), _files);

        Assert.True(vm.ExportCsvCommand.CanExecute(null));
        Assert.False(vm.ExportPngCommand.CanExecute(null));
    }

    // ---------- Подмены сервисов ----------

    private sealed class FakeFileSaveService : IFileSaveService
    {
        public bool Cancel { get; set; }
        public Exception? Error { get; set; }

        public string? LastFileName { get; private set; }
        public string? LastText { get; private set; }
        public byte[]? LastBytes { get; private set; }

        public Task<string?> SaveTextAsync(string suggestedFileName, string content)
        {
            LastText = content;
            return Save(suggestedFileName);
        }

        public Task<string?> SaveBytesAsync(string suggestedFileName, byte[] content)
        {
            LastBytes = content;
            return Save(suggestedFileName);
        }

        private Task<string?> Save(string fileName)
        {
            if (Error is not null) throw Error;
            if (Cancel) return Task.FromResult<string?>(null);
            LastFileName = fileName;
            return Task.FromResult<string?>(fileName);
        }
    }

    private sealed class FakeChartSnapshot : IChartSnapshotProvider
    {
        public byte[]? Png { get; set; } = [0x89, 0x50, 0x4E, 0x47];

        public byte[]? CaptureChartPng() => Png;
    }
}
