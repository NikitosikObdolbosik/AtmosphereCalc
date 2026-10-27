using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtmosphereCalc.Models;
using AtmosphereCalc.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtmosphereCalc.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public const string EmptyFieldError = "Поле не заполнено";
    public const string NotANumberError = "Введите число";
    public const string ExportFileName = "atmosphere_profile.csv";
    public const string ChartXAxisTitle = "h, м";

    private readonly AtmosphereCalculator _calculator;
    private readonly IFileSaveService? _fileSaveService;
    private readonly IChartSnapshotProvider? _chartSnapshot;

    // Параметры последнего расчёта: экспорт описывает то, что на экране, а не текущий ввод.
    private ProfileParameters? _lastParameters;

    private double _altitude;
    private double _step;
    private double _seaLevelTemperature;
    private bool _isSyncing;

    // Конструктор для дизайнера XAML: без диалога сохранения.
    public MainViewModel() : this(new AtmosphereCalculator(), null)
    {
    }

    public MainViewModel(IFileSaveService fileSaveService, IChartSnapshotProvider chartSnapshot)
        : this(new AtmosphereCalculator(), fileSaveService, chartSnapshot)
    {
    }

    public MainViewModel(
        AtmosphereCalculator calculator,
        IFileSaveService? fileSaveService,
        IChartSnapshotProvider? chartSnapshot = null)
    {
        _calculator = calculator;
        _fileSaveService = fileSaveService;
        _chartSnapshot = chartSnapshot;

        Validate();
        Calculate();
    }

    // ---------- Ввод ----------

    /// <summary>Целевая высота, м.</summary>
    [ObservableProperty]
    public partial string AltitudeText { get; set; } = "10000";

    /// <summary>Шаг построения профиля, м.</summary>
    [ObservableProperty]
    public partial string StepText { get; set; } = "500";

    /// <summary>Температура на уровне моря, °C.</summary>
    [ObservableProperty]
    public partial string SeaLevelTemperatureText { get; set; } = "15";

    /// <summary>Значение слайдера высоты; синхронизировано с <see cref="AltitudeText"/>.</summary>
    [ObservableProperty]
    public partial double Altitude { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAltitudeError))]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    public partial string? AltitudeError { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStepError))]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    public partial string? StepError { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSeaLevelTemperatureError))]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    public partial string? SeaLevelTemperatureError { get; private set; }

    public bool HasAltitudeError => AltitudeError is not null;
    public bool HasStepError => StepError is not null;
    public bool HasSeaLevelTemperatureError => SeaLevelTemperatureError is not null;

    /// <summary>Параметры изменились после расчёта — результат на экране устарел.</summary>
    [ObservableProperty]
    public partial bool IsResultOutdated { get; private set; }

    // ---------- Результат ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CurrentAltitudeText), nameof(TemperatureText), nameof(TemperatureKText),
        nameof(PressureText), nameof(PressureHpaText), nameof(DensityText), nameof(LayerText),
        nameof(ChartMarker), nameof(ChartMarkerText))]
    public partial AtmosphereData? Current { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChartPoints))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand), nameof(ExportPngCommand))]
    public partial IReadOnlyList<AtmosphereData> Profile { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ProfileRowViewModel> ProfileRows { get; private set; } = [];

    public string CurrentAltitudeText => Current is null ? "" : $"На высоте {Formatting.Number(Current.Altitude, "#,0.##")} м";
    public string TemperatureText => Current is null ? "" : $"{Formatting.Number(Current.TemperatureC, "0.00")} °C";
    public string TemperatureKText => Current is null ? "" : $"{Formatting.Number(Current.TemperatureK, "0.00")} K";
    public string PressureText => Current is null ? "" : $"{Formatting.Number(Current.Pressure, "#,0.00")} Па";
    public string PressureHpaText => Current is null ? "" : $"{Formatting.Number(Current.PressureHpa, "0.00")} гПа";
    public string DensityText => Current is null ? "" : $"{Formatting.Number(Current.Density, "0.0000")} кг/м³";
    public string LayerText => Current?.Layer.DisplayName() ?? "";

    // ---------- График ----------

    public IReadOnlyList<ChartOption> ChartOptions => ChartOption.All;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChartPoints), nameof(ChartMarker), nameof(ChartMarkerText),
        nameof(ChartYAxisTitle), nameof(ChartFileName))]
    public partial ChartOption SelectedChartOption { get; set; } = ChartOption.All[0];

    /// <summary>Подпись оси Y с единицами, например «P, Па».</summary>
    public string ChartYAxisTitle => SelectedChartOption.AxisTitle;

    /// <summary>Параметры расчёта над графиком: «У земли 15 °C · шаг 500 м».</summary>
    [ObservableProperty]
    public partial string ChartCaption { get; private set; } = "";

    public string ChartFileName => $"atmosphere_{SelectedChartOption.FileKey}.png";

    public IReadOnlyList<ChartPoint> ChartPoints =>
        Profile.Select(d => new ChartPoint(d.Altitude, SelectedChartOption.Selector(d))).ToList();

    public ChartPoint? ChartMarker =>
        Current is null ? null : new ChartPoint(Current.Altitude, SelectedChartOption.Selector(Current));

    public string ChartMarkerText => Current is null
        ? ""
        : $"{Formatting.Number(Current.Altitude, "#,0")} м\n" +
          $"{Formatting.Number(SelectedChartOption.Selector(Current), SelectedChartOption.Format)} {SelectedChartOption.Unit}";

    // ---------- Экспорт ----------

    [ObservableProperty]
    public partial string? ExportStatus { get; private set; }

    // ---------- Команды ----------

    private bool CanCalculate() =>
        AltitudeError is null && StepError is null && SeaLevelTemperatureError is null;

    [RelayCommand(CanExecute = nameof(CanCalculate))]
    private void Calculate()
    {
        _lastParameters = new ProfileParameters(_altitude, _step, _seaLevelTemperature);
        Profile = _calculator.BuildProfile(_altitude, _step, _seaLevelTemperature);
        ChartCaption = $"У земли {Formatting.Number(_seaLevelTemperature, "0.##")} °C · шаг {Formatting.Number(_step, "#,0.##")} м";
        ProfileRows = Profile.Select(ProfileRowViewModel.From).ToList();
        Current = Profile[^1];
        IsResultOutdated = false;
        ExportStatus = null;
    }

    private bool CanExport() => _fileSaveService is not null && Profile.Count > 0;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportCsvAsync() =>
        SaveAsync(() => _fileSaveService!.SaveTextAsync(ExportFileName, ProfileCsvExporter.ToCsv(Profile, _lastParameters)));

    private bool CanExportPng() => CanExport() && _chartSnapshot is not null;

    [RelayCommand(CanExecute = nameof(CanExportPng))]
    private Task ExportPngAsync() =>
        SaveAsync(() =>
        {
            byte[]? png = _chartSnapshot!.CaptureChartPng();
            if (png is null)
                throw new InvalidOperationException("график ещё не отрисован");
            return _fileSaveService!.SaveBytesAsync(ChartFileName, png);
        });

    private async Task SaveAsync(Func<Task<string?>> save)
    {
        try
        {
            string? fileName = await save();
            ExportStatus = fileName is null ? null : $"Сохранено: {fileName}";
        }
        catch (Exception ex)
        {
            ExportStatus = $"Не удалось сохранить файл: {ex.Message}";
        }
    }

    // ---------- Проверка ввода ----------

    partial void OnAltitudeTextChanged(string value) => OnInputChanged();
    partial void OnStepTextChanged(string value) => OnInputChanged();
    partial void OnSeaLevelTemperatureTextChanged(string value) => OnInputChanged();

    partial void OnAltitudeChanged(double value)
    {
        if (_isSyncing) return;
        Sync(() => AltitudeText = Formatting.Number(Math.Round(value), "0"));
    }

    private void OnInputChanged()
    {
        Validate();
        if (Current is not null)
            IsResultOutdated = true;
    }

    private void Validate()
    {
        AltitudeError = ParseError(AltitudeText, out _altitude) ?? AtmosphereCalculator.ValidateAltitude(_altitude);
        SeaLevelTemperatureError = ParseError(SeaLevelTemperatureText, out _seaLevelTemperature)
                                   ?? AtmosphereCalculator.ValidateSeaLevelTemperature(_seaLevelTemperature);

        // Шаг сравнивается с высотой, поэтому проверяется, только если высота корректна.
        StepError = ParseError(StepText, out _step)
                    ?? (AltitudeError is null ? AtmosphereCalculator.ValidateStep(_step, _altitude) : null);

        if (AltitudeError is null && !_isSyncing)
            Sync(() => Altitude = _altitude);
    }

    private static string? ParseError(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return EmptyFieldError;
        return Formatting.TryParse(text, out value) ? null : NotANumberError;
    }

    // Не даёт полю ввода и слайдеру бесконечно обновлять друг друга.
    private void Sync(Action action)
    {
        _isSyncing = true;
        try { action(); }
        finally { _isSyncing = false; }
    }
}
