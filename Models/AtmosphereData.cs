namespace AtmosphereCalc.Models;

/// <summary>
/// Параметры атмосферы на заданной высоте.
/// </summary>
public sealed record AtmosphereData(
    double Altitude,
    double Pressure,
    double Density,
    double TemperatureC,
    double TemperatureK);
