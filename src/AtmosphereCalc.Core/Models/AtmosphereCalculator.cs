using System;
using System.Collections.Generic;
using System.Globalization;

namespace AtmosphereCalc.Models;

/// <summary>
/// Расчёт параметров Международной стандартной атмосферы (ISA)
/// для тропосферы и нижней стратосферы.
/// Модель ничего не знает о UI и может использоваться/тестироваться отдельно.
/// </summary>
public sealed class AtmosphereCalculator
{
    public const double MinAltitude = 0.0;
    public const double MaxAltitude = 25000.0;

    public const double MinStep = 100.0;

    public const double StandardSeaLevelTemperatureC = 15.0;
    public const double MinSeaLevelTemperatureC = -50.0;
    public const double MaxSeaLevelTemperatureC = 50.0;

    public const double SeaLevelPressure = 101325.0;

    public const string NotFiniteError = "Значение должно быть конечным числом";
    public const double TropopauseAltitude = 11000.0;

    private const double KelvinOffset = 273.15;
    private const double Gravity = 9.80665;
    private const double LapseRate = 0.0065;
    private const double GasConstant = 8.31446;
    private const double MolarMass = 0.0289644;
    private const double SpecificGasConstant = GasConstant / MolarMass;
    private const double BarometricExponent = Gravity * MolarMass / (GasConstant * LapseRate);

    public static bool IsInRange(double altitude) =>
        altitude >= MinAltitude && altitude <= MaxAltitude;

    /// <summary>Сообщение об ошибке для целевой высоты или null, если высота допустима.</summary>
    public static string? ValidateAltitude(double altitude)
    {
        if (!double.IsFinite(altitude))
            return NotFiniteError;
        if (altitude < MinAltitude)
            return "Высота не может быть отрицательной";
        if (altitude > MaxAltitude)
            return $"Выход за пределы модели: максимум {Format(MaxAltitude)} м";
        return null;
    }

    /// <summary>
    /// Сообщение об ошибке для шага построения или null, если шаг допустим.
    /// Шаг — от 100 м до целевой высоты (для высот ниже 100 м допускается шаг 100 м).
    /// </summary>
    public static string? ValidateStep(double step, double altitude)
    {
        if (!double.IsFinite(step))
            return NotFiniteError;
        if (step < MinStep || step > Math.Max(altitude, MinStep))
            return $"Шаг должен быть от {Format(MinStep)} м до целевой высоты";
        return null;
    }

    /// <summary>Сообщение об ошибке для температуры у земли или null, если она допустима.</summary>
    public static string? ValidateSeaLevelTemperature(double celsius)
    {
        if (!double.IsFinite(celsius))
            return NotFiniteError;
        if (celsius < MinSeaLevelTemperatureC || celsius > MaxSeaLevelTemperatureC)
            return $"Температура у земли: от {Format(MinSeaLevelTemperatureC)} до +{Format(MaxSeaLevelTemperatureC)} °C";
        return null;
    }

    public AtmosphereData Calculate(double altitude, double seaLevelTemperatureC = StandardSeaLevelTemperatureC)
    {
        if (ValidateAltitude(altitude) is { } altitudeError)
            throw new ArgumentOutOfRangeException(nameof(altitude), altitude, altitudeError);
        if (ValidateSeaLevelTemperature(seaLevelTemperatureC) is { } temperatureError)
            throw new ArgumentOutOfRangeException(nameof(seaLevelTemperatureC), seaLevelTemperatureC, temperatureError);

        double seaLevelTemperature = seaLevelTemperatureC + KelvinOffset;
        double temperature, pressure;
        AtmosphereLayer layer;

        if (altitude < TropopauseAltitude)
        {
            layer = AtmosphereLayer.Troposphere;
            temperature = seaLevelTemperature - LapseRate * altitude;
            pressure = TroposphericPressure(temperature, seaLevelTemperature);
        }
        else
        {
            // Выше тропопаузы температура постоянна, давление падает экспоненциально
            // от значения на границе слоёв.
            layer = AtmosphereLayer.Stratosphere;
            temperature = seaLevelTemperature - LapseRate * TropopauseAltitude;
            double tropopausePressure = TroposphericPressure(temperature, seaLevelTemperature);
            pressure = tropopausePressure * Math.Exp(
                -Gravity * MolarMass * (altitude - TropopauseAltitude) / (GasConstant * temperature));
        }

        double density = pressure / (SpecificGasConstant * temperature);

        return new AtmosphereData(
            Altitude: altitude,
            Pressure: Math.Round(pressure, 2),
            Density: Math.Round(density, 4),
            TemperatureC: Math.Round(temperature - KelvinOffset, 2),
            TemperatureK: Math.Round(temperature, 2),
            Layer: layer);
    }

    /// <summary>
    /// Профиль атмосферы от 0 до <paramref name="targetAltitude"/> с шагом <paramref name="step"/>.
    /// Целевая высота всегда входит в профиль последней точкой.
    /// </summary>
    public IReadOnlyList<AtmosphereData> BuildProfile(
        double targetAltitude,
        double step,
        double seaLevelTemperatureC = StandardSeaLevelTemperatureC)
    {
        // Высоту проверяем явно: при NaN цикл ниже молча вернул бы одну точку на уровне моря.
        if (ValidateAltitude(targetAltitude) is { } altitudeError)
            throw new ArgumentOutOfRangeException(nameof(targetAltitude), targetAltitude, altitudeError);
        if (ValidateStep(step, targetAltitude) is { } stepError)
            throw new ArgumentOutOfRangeException(nameof(step), step, stepError);

        var profile = new List<AtmosphereData>();
        int count = (int)Math.Floor(targetAltitude / step);

        for (int i = 0; i <= count; i++)
            profile.Add(Calculate(i * step, seaLevelTemperatureC));

        if (profile[^1].Altitude < targetAltitude)
            profile.Add(Calculate(targetAltitude, seaLevelTemperatureC));

        return profile;
    }

    private static double TroposphericPressure(double temperature, double seaLevelTemperature) =>
        SeaLevelPressure * Math.Pow(temperature / seaLevelTemperature, BarometricExponent);

    // 25000 → «25 000», независимо от языка системы.
    private static string Format(double value) =>
        value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
}
