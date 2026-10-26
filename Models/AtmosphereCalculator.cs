using System;
using System.Collections.Generic;

namespace AtmosphereCalc.Models;

/// <summary>
/// Расчёт параметров Международной стандартной атмосферы (ISA).
/// Модель ничего не знает о UI и может использоваться/тестироваться отдельно.
/// </summary>
public sealed class AtmosphereCalculator
{
    public const double MinAltitude = 0.0;
    public const double MaxAltitude = 20000.0;
    public const double SeaLevelPressure = 101325.0;

    private const double SeaLevelTemperature = 288.15;
    private const double Gravity = 9.80665;
    private const double LapseRate = 0.0065;
    private const double GasConstant = 8.31446;
    private const double MolarMass = 0.0289644;
    private const double SpecificGasConstant = GasConstant / MolarMass;

    private const double TropopauseAltitude = 11000.0;
    private const double TropopauseTemperature = 216.65;
    private const double TropopausePressure = 22632.1;

    public static bool IsInRange(double altitude) =>
        altitude >= MinAltitude && altitude <= MaxAltitude;

    public AtmosphereData Calculate(double altitude)
    {
        if (!IsInRange(altitude))
            throw new ArgumentOutOfRangeException(nameof(altitude), altitude,
                $"Высота должна быть от {MinAltitude} до {MaxAltitude} м");

        double temperature, pressure;

        if (altitude < TropopauseAltitude)
        {
            temperature = SeaLevelTemperature - LapseRate * altitude;
            pressure = SeaLevelPressure * Math.Pow(
                1.0 - LapseRate * altitude / SeaLevelTemperature,
                Gravity * MolarMass / (GasConstant * LapseRate));
        }
        else
        {
            temperature = TropopauseTemperature;
            pressure = TropopausePressure * Math.Exp(
                -Gravity * MolarMass * (altitude - TropopauseAltitude) / (GasConstant * temperature));
        }

        double density = pressure / (SpecificGasConstant * temperature);

        return new AtmosphereData(
            Altitude: altitude,
            Pressure: Math.Round(pressure, 2),
            Density: Math.Round(density, 4),
            TemperatureC: Math.Round(temperature - 273.15, 2),
            TemperatureK: Math.Round(temperature, 2));
    }

    /// <summary>
    /// Профиль атмосферы от 0 до <see cref="MaxAltitude"/> с равным шагом.
    /// </summary>
    public IReadOnlyList<AtmosphereData> BuildProfile(int segments)
    {
        var profile = new List<AtmosphereData>(segments + 1);
        for (int i = 0; i <= segments; i++)
            profile.Add(Calculate(MaxAltitude / segments * i));
        return profile;
    }
}
