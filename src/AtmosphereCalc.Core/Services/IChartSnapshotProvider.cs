namespace AtmosphereCalc.Services;

/// <summary>
/// Снимок графика в PNG. Рисует его View, а ViewModel только просит снимок и сохраняет файл,
/// поэтому ViewModel по-прежнему не зависит от Avalonia.
/// </summary>
public interface IChartSnapshotProvider
{
    /// <summary>PNG-файл графика или null, если график ещё не отрисован.</summary>
    byte[]? CaptureChartPng();
}
