using System;
using System.Linq;
using AtmosphereCalc.Models;

namespace AtmosphereCalc.Tests.Models;

public class AtmosphereCalculatorTests
{
    private readonly AtmosphereCalculator _calculator = new();

    [Fact]
    public void Calculate_SeaLevel_ReturnsStandardValues()
    {
        var result = _calculator.Calculate(0);

        Assert.Equal(0, result.Altitude);
        Assert.Equal(101325.0, result.Pressure, 2);
        Assert.Equal(1.2250, result.Density, 4);
        Assert.Equal(15.00, result.TemperatureC, 2);
        Assert.Equal(288.15, result.TemperatureK, 2);
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
    [InlineData(20000)]
    public void Calculate_Stratosphere_TemperatureIsConstant(double altitude)
    {
        var result = _calculator.Calculate(altitude);

        Assert.Equal(-56.5, result.TemperatureC, 2);
        Assert.Equal(216.65, result.TemperatureK, 2);
    }

    // Табличные значения ISA, допуск — погрешность модели и округления.
    [Theory]
    [InlineData(5000, 54020, 50)]
    [InlineData(11000, 22632, 5)]
    [InlineData(20000, 5475, 10)]
    public void Calculate_Pressure_MatchesIsaTable(double altitude, double expectedPa, double tolerance)
    {
        var result = _calculator.Calculate(altitude);

        Assert.InRange(result.Pressure, expectedPa - tolerance, expectedPa + tolerance);
    }

    [Fact]
    public void Calculate_Tropopause_IsContinuous()
    {
        var below = _calculator.Calculate(10999.9);
        var above = _calculator.Calculate(11000);

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
    [InlineData(-0.1)]
    [InlineData(-1000)]
    [InlineData(20000.1)]
    [InlineData(30000)]
    public void Calculate_OutOfRange_Throws(double altitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(altitude));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(20000, true)]
    [InlineData(-1, false)]
    [InlineData(20001, false)]
    public void IsInRange_ChecksBounds(double altitude, bool expected)
    {
        Assert.Equal(expected, AtmosphereCalculator.IsInRange(altitude));
    }

    [Fact]
    public void BuildProfile_ReturnsEvenlySpacedPointsFromZeroToMax()
    {
        var profile = _calculator.BuildProfile(100);

        Assert.Equal(101, profile.Count);
        Assert.Equal(0, profile[0].Altitude);
        Assert.Equal(AtmosphereCalculator.MaxAltitude, profile[^1].Altitude);
        Assert.Equal(200, profile[1].Altitude - profile[0].Altitude, 6);
    }

    [Fact]
    public void BuildProfile_PressureDecreasesWithAltitude()
    {
        var pressures = _calculator.BuildProfile(50).Select(p => p.Pressure).ToList();

        for (int i = 1; i < pressures.Count; i++)
            Assert.True(pressures[i] < pressures[i - 1], $"Давление выросло на шаге {i}");
    }
}
