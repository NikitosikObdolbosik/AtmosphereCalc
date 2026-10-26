using System.Globalization;
using AtmosphereCalc.Converters;
using Avalonia.Media;

namespace AtmosphereCalc.Tests.Converters;

public class TemperatureToBrushConverterTests
{
    private static readonly Color Cold = Color.Parse("#0000FF");
    private static readonly Color Hot = Color.Parse("#FF0000");

    private readonly TemperatureToBrushConverter _converter = new()
    {
        ColdStart = Cold, ColdEnd = Cold,
        HotStart = Hot, HotEnd = Hot
    };

    private LinearGradientBrush Convert(object? value) =>
        Assert.IsType<LinearGradientBrush>(
            _converter.Convert(value, typeof(IBrush), null, CultureInfo.InvariantCulture));

    [Fact]
    public void SeaLevelTemperature_GivesHotColors()
    {
        var brush = Convert(15.0);

        Assert.Equal(Hot, brush.GradientStops[0].Color);
        Assert.Equal(Hot, brush.GradientStops[1].Color);
    }

    [Fact]
    public void StratosphereTemperature_GivesColdColors()
    {
        var brush = Convert(-56.5);

        Assert.Equal(Cold, brush.GradientStops[0].Color);
    }

    [Fact]
    public void OutOfRangeTemperature_IsClamped()
    {
        Assert.Equal(Hot, Convert(40.0).GradientStops[0].Color);
        Assert.Equal(Cold, Convert(-80.0).GradientStops[0].Color);
    }

    [Fact]
    public void MiddleTemperature_GivesMixedColor()
    {
        var color = Convert(-20.75).GradientStops[0].Color;

        Assert.InRange(color.R, 120, 135);
        Assert.InRange(color.B, 120, 135);
    }

    [Fact]
    public void NonDouble_ReturnsNull()
    {
        Assert.Null(_converter.Convert("15", typeof(IBrush), null, CultureInfo.InvariantCulture));
        Assert.Null(_converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture));
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
    [InlineData(true, "Red")]
    [InlineData(false, "Black")]
    [InlineData(null, "Black")]
    public void Convert_ReturnsBrushForValue(bool? value, string expected)
    {
        var result = _converter.Convert(value, typeof(IBrush), null, CultureInfo.InvariantCulture);

        Assert.Same(expected == "Red" ? Brushes.Red : Brushes.Black, result);
    }
}
