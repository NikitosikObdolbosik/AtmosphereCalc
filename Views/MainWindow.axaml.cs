using AtmosphereCalc.Services;
using Avalonia.Controls;

namespace AtmosphereCalc.Views;

public partial class MainWindow : Window, IChartSnapshotProvider
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public byte[]? CaptureChartPng() => Chart.CapturePng();
}
