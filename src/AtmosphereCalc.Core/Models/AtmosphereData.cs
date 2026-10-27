namespace AtmosphereCalc.Models;

/// <summary>
/// Параметры атмосферы на заданной высоте.
/// </summary>
public sealed record AtmosphereData(
    double Altitude,
    double Pressure,
    double Density,
    double TemperatureC,
    double TemperatureK,
    AtmosphereLayer Layer)
{
    /// <summary>Давление в гектопаскалях.</summary>
    public double PressureHpa => Pressure / 100.0;
}
