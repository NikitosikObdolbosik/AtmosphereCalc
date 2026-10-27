using System;
using AtmosphereCalc.Models;

namespace AtmosphereCalc.ViewModels;

/// <summary>Точка графика: X — высота (м), Y — значение выбранной величины.</summary>
public readonly record struct ChartPoint(double X, double Y);

/// <summary>Величина, которую можно показать на графике профиля.</summary>
/// <param name="Symbol">Обозначение на оси: P, ρ, T.</param>
/// <param name="FileKey">Часть имени файла при сохранении графика.</param>
public sealed record ChartOption(
    string Title,
    string Symbol,
    string Unit,
    string Format,
    string FileKey,
    Func<AtmosphereData, double> Selector)
{
    /// <summary>Подпись оси Y, например «P, Па».</summary>
    public string AxisTitle => $"{Symbol}, {Unit}";

    public override string ToString() => Title;

    public static ChartOption[] All { get; } =
    [
        new("Давление", "P", "Па", "#,0", "pressure", d => d.Pressure),
        new("Плотность", "ρ", "кг/м³", "0.0000", "density", d => d.Density),
        new("Температура", "T", "°C", "0.00", "temperature", d => d.TemperatureC)
    ];
}
