using System;
using System.Collections.Generic;
using System.Globalization;
using AtmosphereCalc.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtmosphereCalc.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int ProfileSegments = 100;

    private readonly AtmosphereCalculator _calculator;
    private bool _isSyncing;

    public MainViewModel() : this(new AtmosphereCalculator())
    {
    }

    public MainViewModel(AtmosphereCalculator calculator)
    {
        _calculator = calculator;
        PressureProfile = _calculator.BuildProfile(ProfileSegments);
        UpdateResult(0);
    }

    public double MinAltitude => AtmosphereCalculator.MinAltitude;
    public double MaxAltitude => AtmosphereCalculator.MaxAltitude;
    public double MaxPressure => AtmosphereCalculator.SeaLevelPressure;

    /// <summary>Точки для графика давления (0..MaxAltitude).</summary>
    public IReadOnlyList<AtmosphereData> PressureProfile { get; }

    /// <summary>Текст в поле ввода высоты.</summary>
    [ObservableProperty]
    public partial string AltitudeText { get; set; } = "0";

    /// <summary>Значение слайдера высоты.</summary>
    [ObservableProperty]
    public partial double Altitude { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PressureText), nameof(DensityText), nameof(TemperatureText))]
    public partial AtmosphereData? Current { get; set; }

    public string PressureText => Current is null
        ? string.Empty
        : $"{Current.Pressure.ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(",", " ")} Па";

    public string DensityText => Current is null
        ? string.Empty
        : $"{Current.Density.ToString("0.0000", CultureInfo.InvariantCulture)} кг/м³";

    public string TemperatureText => Current is null
        ? string.Empty
        : $"{Current.TemperatureC.ToString("0.00", CultureInfo.InvariantCulture)} °C";

    partial void OnAltitudeTextChanged(string value)
    {
        if (_isSyncing || string.IsNullOrWhiteSpace(value)) return;

        if (!double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double altitude))
        {
            ErrorMessage = "Только числа";
            return;
        }

        if (!AtmosphereCalculator.IsInRange(altitude))
        {
            ErrorMessage = $"От {MinAltitude} до {MaxAltitude} м";
            return;
        }

        ErrorMessage = null;
        Sync(() => Altitude = altitude);
        UpdateResult(altitude);
    }

    partial void OnAltitudeChanged(double value)
    {
        if (_isSyncing) return;

        double altitude = Math.Round(value);
        ErrorMessage = null;
        Sync(() => AltitudeText = altitude.ToString(CultureInfo.InvariantCulture));
        UpdateResult(altitude);
    }

    private void UpdateResult(double altitude) => Current = _calculator.Calculate(altitude);

    // Не даёт полю ввода и слайдеру бесконечно обновлять друг друга.
    private void Sync(Action action)
    {
        _isSyncing = true;
        try { action(); }
        finally { _isSyncing = false; }
    }
}
