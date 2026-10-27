namespace AtmosphereCalc.Models;

/// <summary>
/// Исходные данные, по которым построен профиль. Записываются в экспорт,
/// чтобы по файлу можно было повторить расчёт.
/// </summary>
public sealed record ProfileParameters(
    double TargetAltitude,
    double Step,
    double SeaLevelTemperatureC);
