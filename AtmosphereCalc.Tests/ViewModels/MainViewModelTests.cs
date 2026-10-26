using System.Collections.Generic;
using AtmosphereCalc.Models;
using AtmosphereCalc.ViewModels;

namespace AtmosphereCalc.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly MainViewModel _vm = new();

    [Fact]
    public void Constructor_ShowsSeaLevelValues()
    {
        Assert.Equal("0", _vm.AltitudeText);
        Assert.Equal(0, _vm.Altitude);
        Assert.False(_vm.HasError);
        Assert.Equal("101 325.00 Па", _vm.PressureText);
        Assert.Equal("1.2250 кг/м³", _vm.DensityText);
        Assert.Equal("15.00 °C", _vm.TemperatureText);
    }

    [Fact]
    public void Constructor_BuildsPressureProfile()
    {
        Assert.Equal(101, _vm.PressureProfile.Count);
        Assert.Equal(AtmosphereCalculator.MaxAltitude, _vm.MaxAltitude);
        Assert.Equal(AtmosphereCalculator.SeaLevelPressure, _vm.MaxPressure);
    }

    [Fact]
    public void AltitudeText_Valid_UpdatesSliderAndResult()
    {
        _vm.AltitudeText = "5000";

        Assert.Equal(5000, _vm.Altitude);
        Assert.Equal(5000, _vm.Current!.Altitude);
        Assert.Equal("-17.50 °C", _vm.TemperatureText);
        Assert.False(_vm.HasError);
    }

    [Fact]
    public void AltitudeText_WithComma_IsParsed()
    {
        _vm.AltitudeText = "1500,5";

        Assert.Equal(1500.5, _vm.Altitude);
        Assert.False(_vm.HasError);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12a")]
    public void AltitudeText_NotANumber_ShowsError(string text)
    {
        _vm.AltitudeText = text;

        Assert.True(_vm.HasError);
        Assert.Equal("Только числа", _vm.ErrorMessage);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("20001")]
    public void AltitudeText_OutOfRange_ShowsErrorAndKeepsLastResult(string text)
    {
        _vm.AltitudeText = "3000";
        var before = _vm.Current;

        _vm.AltitudeText = text;

        Assert.True(_vm.HasError);
        Assert.Contains("20000", _vm.ErrorMessage);
        Assert.Same(before, _vm.Current);
        Assert.Equal(3000, _vm.Altitude);
    }

    [Fact]
    public void AltitudeText_Empty_IsIgnored()
    {
        _vm.AltitudeText = "3000";

        _vm.AltitudeText = "";

        Assert.False(_vm.HasError);
        Assert.Equal(3000, _vm.Current!.Altitude);
    }

    [Fact]
    public void AltitudeText_ValidAfterError_ClearsError()
    {
        _vm.AltitudeText = "abc";

        _vm.AltitudeText = "100";

        Assert.False(_vm.HasError);
        Assert.Null(_vm.ErrorMessage);
    }

    [Fact]
    public void Altitude_FromSlider_RoundsAndUpdatesText()
    {
        _vm.Altitude = 1234.4;

        Assert.Equal("1234", _vm.AltitudeText);
        Assert.Equal(1234, _vm.Current!.Altitude);
    }

    [Fact]
    public void Altitude_FromSlider_ClearsError()
    {
        _vm.AltitudeText = "abc";

        _vm.Altitude = 2000;

        Assert.False(_vm.HasError);
        Assert.Equal("2000", _vm.AltitudeText);
    }

    [Fact]
    public void ChangingAltitude_RaisesPropertyChangedForDisplayedTexts()
    {
        var changed = new List<string?>();
        _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _vm.AltitudeText = "8000";

        Assert.Contains(nameof(MainViewModel.Current), changed);
        Assert.Contains(nameof(MainViewModel.PressureText), changed);
        Assert.Contains(nameof(MainViewModel.DensityText), changed);
        Assert.Contains(nameof(MainViewModel.TemperatureText), changed);
    }

    [Fact]
    public void Error_RaisesPropertyChangedForHasError()
    {
        var changed = new List<string?>();
        _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _vm.AltitudeText = "abc";

        Assert.Contains(nameof(MainViewModel.HasError), changed);
    }
}
