using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using AtmosphereCalc.Converters;
using Avalonia.Media;

namespace AtmosphereCalc.UI.Tests.Converters;

public class TemperatureToBrushConverterTests
{
    private static readonly Color Cold = Color.Parse("#0000FF");
    private static readonly Color Hot = Color.Parse("#FF0000");

    // Цвета обеих точек градиента. Кисть создаётся и читается в одном потоке, см. UiThread.
    private static (Color Start, Color End)? Convert(object? value) => UiThread.Run(() =>
    {
        var converter = new TemperatureToBrushConverter
        {
            ColdStart = Cold, ColdEnd = Cold,
            HotStart = Hot, HotEnd = Hot
        };
        var result = converter.Convert(value, typeof(IBrush), null, CultureInfo.InvariantCulture);
        if (result is null) return ((Color, Color)?)null;

        var brush = Assert.IsType<LinearGradientBrush>(result);
        Assert.Equal(2, brush.GradientStops.Count);
        return (brush.GradientStops[0].Color, brush.GradientStops[1].Color);
    });

    [Fact]
    public void SeaLevelTemperature_GivesHotColors()
    {
        var colors = Convert(15.0);

        Assert.Equal((Hot, Hot), colors);
    }

    [Fact]
    public void StratosphereTemperature_GivesColdColors()
    {
        var colors = Convert(-56.5);

        Assert.Equal((Cold, Cold), colors);
    }

    [Fact]
    public void OutOfRangeTemperature_IsClamped()
    {
        Assert.Equal(Hot, Convert(40.0)!.Value.Start);
        Assert.Equal(Cold, Convert(-121.5)!.Value.Start);
    }

    [Fact]
    public void MiddleTemperature_GivesMixedColor()
    {
        // −20,75 °C — ровно середина диапазона −56,5…+15 °C.
        var color = Convert(-20.75)!.Value.Start;

        Assert.InRange(color.R, 120, 135);
        Assert.InRange(color.B, 120, 135);
    }

    [Fact]
    public void NonDouble_ReturnsNull()
    {
        Assert.Null(Convert("15"));
        Assert.Null(Convert(null));
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        var converter = new TemperatureToBrushConverter();

        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(null, typeof(double), null, CultureInfo.InvariantCulture));
    }
}

public class BoolToBrushConverterTests
{
    private readonly BoolToBrushConverter _converter = new()
    {
        TrueBrush = Brushes.Red,
        FalseBrush = Brushes.Black
    };

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, false)]
    [InlineData("true", false)]
    public void Convert_ReturnsTrueBrushOnlyForTrue(object? value, bool expectTrueBrush)
    {
        var result = _converter.Convert(value, typeof(IBrush), null, CultureInfo.InvariantCulture);

        Assert.Same(expectTrueBrush ? Brushes.Red : Brushes.Black, result);
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Brushes.Red, typeof(bool), null, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Объекты Avalonia (кисти) привязаны к потоку, в котором их впервые тронули,
/// а xUnit запускает тесты в разных потоках. Поэтому всё, что создаёт кисти, идёт через один поток.
/// </summary>
internal static class UiThread
{
    private static readonly BlockingCollection<Action> Queue = new();

    static UiThread()
    {
        var thread = new Thread(() =>
        {
            foreach (var action in Queue.GetConsumingEnumerable())
                action();
        }) { IsBackground = true, Name = "Avalonia test thread" };
        thread.Start();
    }

    public static T Run<T>(Func<T> func)
    {
        T result = default!;
        Exception? error = null;
        using var done = new ManualResetEventSlim();

        Queue.Add(() =>
        {
            try { result = func(); }
            catch (Exception ex) { error = ex; }
            finally { done.Set(); }
        });
        done.Wait();

        if (error is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        return result;
    }
}
