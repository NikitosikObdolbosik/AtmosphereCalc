using AtmosphereCalc.Models;

namespace AtmosphereCalc.ViewModels;

/// <summary>Строка таблицы профиля, уже отформатированная для показа.</summary>
public sealed record ProfileRowViewModel(
    string Altitude,
    string TemperatureC,
    string TemperatureK,
    string Pressure,
    string PressureHpa,
    string Density,
    string Layer)
{
    public static ProfileRowViewModel From(AtmosphereData data) => new(
        Altitude: Formatting.Number(data.Altitude, "#,0"),
        TemperatureC: Formatting.Number(data.TemperatureC, "0.00"),
        TemperatureK: Formatting.Number(data.TemperatureK, "0.00"),
        Pressure: Formatting.Number(data.Pressure, "#,0.00"),
        PressureHpa: Formatting.Number(data.PressureHpa, "0.00"),
        Density: Formatting.Number(data.Density, "0.0000"),
        Layer: data.Layer.DisplayName());
}
