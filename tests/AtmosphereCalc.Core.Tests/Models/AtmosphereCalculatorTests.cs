using System;
using System.Linq;
using AtmosphereCalc.Models;

namespace AtmosphereCalc.Core.Tests.Models;

public class AtmosphereCalculatorTests
{
    private readonly AtmosphereCalculator _calculator = new();

    // ---------- Calculate ----------

    [Fact]
    public void Calculate_SeaLevel_ReturnsStandardValues()
    {
        var result = _calculator.Calculate(0);

        Assert.Equal(0, result.Altitude);
        Assert.Equal(101325.0, result.Pressure, 2);
        Assert.Equal(1013.25, result.PressureHpa, 2);
        Assert.Equal(1.2250, result.Density, 4);
        Assert.Equal(15.00, result.TemperatureC, 2);
        Assert.Equal(288.15, result.TemperatureK, 2);
        Assert.Equal(AtmosphereLayer.Troposphere, result.Layer);
    }

    [Theory]
    [InlineData(1000, 8.5)]
    [InlineData(5000, -17.5)]
    [InlineData(10000, -50.0)]
    public void Calculate_Troposphere_TemperatureDropsBy6_5PerKm(double altitude, double expectedCelsius)
    {
        var result = _calculator.Calculate(altitude);

        Assert.Equal(expectedCelsius, result.TemperatureC, 2);
    }

    [Theory]
    [InlineData(11000)]
    [InlineData(15000)]
    [InlineData(25000)]
    public void Calculate_Stratosphere_TemperatureIsConstant(double altitude)
    {
        var result = _calculator.Calculate(altitude);

        Assert.Equal(-56.5, result.TemperatureC, 2);
        Assert.Equal(216.65, result.TemperatureK, 2);
    }

    // Табличные значения ISA. Модель считает высоту геометрической, а таблица ISA — геопотенциальной,
    // поэтому на 25 км расхождение около 1 % (таблица — 2511 Па, модель — 2489 Па).
    [Theory]
    [InlineData(5000, 54020, 5)]
    [InlineData(11000, 22632, 5)]
    [InlineData(20000, 5475, 5)]
    [InlineData(25000, 2511, 30)]
    public void Calculate_Pressure_MatchesIsaTable(double altitude, double expectedPa, double tolerance)
    {
        var result = _calculator.Calculate(altitude);

        Assert.InRange(result.Pressure, expectedPa - tolerance, expectedPa + tolerance);
    }

    [Fact]
    public void Calculate_Tropopause_IsContinuous()
    {
        var below = _calculator.Calculate(10999.9);
        var above = _calculator.Calculate(AtmosphereCalculator.TropopauseAltitude);

        Assert.InRange(Math.Abs(below.Pressure - above.Pressure), 0, 5);
        Assert.InRange(Math.Abs(below.TemperatureK - above.TemperatureK), 0, 0.01);
    }

    [Fact]
    public void Calculate_Density_FollowsIdealGasLaw()
    {
        var result = _calculator.Calculate(7500);
        double expected = result.Pressure / (287.05 * result.TemperatureK);

        Assert.Equal(expected, result.Density, 3);
    }

    [Theory]
    [InlineData(0, AtmosphereLayer.Troposphere)]
    [InlineData(10999, AtmosphereLayer.Troposphere)]
    [InlineData(11000, AtmosphereLayer.Stratosphere)]
    [InlineData(25000, AtmosphereLayer.Stratosphere)]
    public void Calculate_ReturnsLayerByTropopause(double altitude, AtmosphereLayer expected)
    {
        Assert.Equal(expected, _calculator.Calculate(altitude).Layer);
    }

    [Fact]
    public void Calculate_CustomSeaLevelTemperature_ShiftsWholeProfile()
    {
        var ground = _calculator.Calculate(0, seaLevelTemperatureC: 25);
        var tropopause = _calculator.Calculate(11000, seaLevelTemperatureC: 25);
        var stratosphere = _calculator.Calculate(20000, seaLevelTemperatureC: 25);

        Assert.Equal(25.00, ground.TemperatureC, 2);
        Assert.Equal(101325.0, ground.Pressure, 2);
        // Тёплый воздух у земли менее плотный, чем при стандартных +15 °C.
        Assert.True(ground.Density < 1.2250);
        Assert.Equal(25 - 71.5, tropopause.TemperatureC, 2);
        Assert.Equal(tropopause.TemperatureC, stratosphere.TemperatureC, 2);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1000)]
    [InlineData(25000.1)]
    [InlineData(30000)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Calculate_InvalidAltitude_Throws(double altitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(altitude));
    }

    [Theory]
    [InlineData(-50.1)]
    [InlineData(50.1)]
    [InlineData(double.NaN)]
    public void Calculate_InvalidSeaLevelTemperature_Throws(double celsius)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(1000, celsius));
    }

    // ---------- Проверка параметров ----------

    [Theory]
    [InlineData(0, true)]
    [InlineData(25000, true)]
    [InlineData(-1, false)]
    [InlineData(25001, false)]
    public void IsInRange_ChecksBounds(double altitude, bool expected)
    {
        Assert.Equal(expected, AtmosphereCalculator.IsInRange(altitude));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12345.6)]
    [InlineData(25000)]
    public void ValidateAltitude_Valid_ReturnsNull(double altitude)
    {
        Assert.Null(AtmosphereCalculator.ValidateAltitude(altitude));
    }

    [Fact]
    public void ValidateAltitude_Negative_ExplainsWhy()
    {
        Assert.Equal("Высота не может быть отрицательной", AtmosphereCalculator.ValidateAltitude(-1));
    }

    [Fact]
    public void ValidateAltitude_AboveModel_ShowsLimit()
    {
        Assert.Contains("25 000", AtmosphereCalculator.ValidateAltitude(25001));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ValidateAltitude_NotFinite_ReturnsNotFiniteError(double altitude)
    {
        Assert.Equal(AtmosphereCalculator.NotFiniteError, AtmosphereCalculator.ValidateAltitude(altitude));
    }

    [Theory]
    [InlineData(100, 1000, true)]
    [InlineData(1000, 1000, true)]
    [InlineData(99.9, 1000, false)]
    [InlineData(1000.1, 1000, false)]
    // Ниже 100 м шаг 100 м всё равно допустим, иначе корректного шага не было бы.
    [InlineData(100, 50, true)]
    [InlineData(100, 0, true)]
    [InlineData(double.NaN, 1000, false)]
    public void ValidateStep_ChecksRange(double step, double altitude, bool isValid)
    {
        Assert.Equal(isValid, AtmosphereCalculator.ValidateStep(step, altitude) is null);
    }

    [Theory]
    [InlineData(-50, true)]
    [InlineData(15, true)]
    [InlineData(50, true)]
    [InlineData(-50.1, false)]
    [InlineData(50.1, false)]
    [InlineData(double.NaN, false)]
    public void ValidateSeaLevelTemperature_ChecksRange(double celsius, bool isValid)
    {
        Assert.Equal(isValid, AtmosphereCalculator.ValidateSeaLevelTemperature(celsius) is null);
    }

    // ---------- BuildProfile ----------

    [Fact]
    public void BuildProfile_EvenStep_ReturnsPointsFromZeroToTarget()
    {
        var profile = _calculator.BuildProfile(10000, 500);

        Assert.Equal(21, profile.Count);
        Assert.Equal(0, profile[0].Altitude);
        Assert.Equal(10000, profile[^1].Altitude);
        Assert.All(profile.Zip(profile.Skip(1)), pair =>
            Assert.Equal(500, pair.Second.Altitude - pair.First.Altitude, 6));
    }

    [Fact]
    public void BuildProfile_UnevenStep_AddsTargetAsLastPoint()
    {
        var altitudes = _calculator.BuildProfile(1250, 500).Select(p => p.Altitude);

        Assert.Equal(new double[] { 0, 500, 1000, 1250 }, altitudes);
    }

    [Fact]
    public void BuildProfile_ZeroAltitude_ReturnsSingleSeaLevelPoint()
    {
        var profile = _calculator.BuildProfile(0, 100);

        Assert.Single(profile);
        Assert.Equal(0, profile[0].Altitude);
    }

    [Fact]
    public void BuildProfile_PassesSeaLevelTemperatureToEveryPoint()
    {
        var profile = _calculator.BuildProfile(2000, 1000, seaLevelTemperatureC: -10);

        Assert.Equal(-10.0, profile[0].TemperatureC, 2);
        Assert.Equal(-16.5, profile[1].TemperatureC, 2);
        Assert.Equal(-23.0, profile[2].TemperatureC, 2);
    }

    [Fact]
    public void BuildProfile_PressureAndDensityDecreaseWithAltitude()
    {
        var profile = _calculator.BuildProfile(AtmosphereCalculator.MaxAltitude, 100);

        for (int i = 1; i < profile.Count; i++)
        {
            Assert.True(profile[i].Pressure < profile[i - 1].Pressure, $"Давление выросло на шаге {i}");
            Assert.True(profile[i].Density <= profile[i - 1].Density, $"Плотность выросла на шаге {i}");
        }
    }

    [Theory]
    [InlineData(double.NaN, 500)]
    [InlineData(-1, 500)]
    [InlineData(30000, 500)]
    [InlineData(10000, 50)]
    [InlineData(10000, 20000)]
    public void BuildProfile_InvalidParameters_Throws(double target, double step)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.BuildProfile(target, step));
    }
}
