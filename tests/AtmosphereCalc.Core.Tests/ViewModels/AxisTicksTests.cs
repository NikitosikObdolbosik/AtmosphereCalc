using System;
using System.Linq;
using AtmosphereCalc.ViewModels;

namespace AtmosphereCalc.Core.Tests.ViewModels;

public class AxisTicksTests
{
    [Theory]
    [InlineData(0, 25000, 6, 5000)]
    [InlineData(0, 10000, 6, 2000)]
    [InlineData(3, 97, 5, 50)]
    [InlineData(0, 0.04, 5, 0.01)]
    public void For_PicksRoundStep(double min, double max, int count, double expectedStep)
    {
        var ticks = AxisTicks.For(min, max, count);

        Assert.Equal(expectedStep, ticks.Step, 9);
    }

    [Theory]
    [InlineData(0, 25000)]
    [InlineData(3, 97)]
    [InlineData(-56.5, 15)]
    [InlineData(2488.78, 101325)]
    [InlineData(0.04, 1.225)]
    public void For_CoversDataRange(double min, double max)
    {
        var ticks = AxisTicks.For(min, max, 6);

        Assert.True(ticks.Min <= min);
        Assert.True(ticks.Max >= max);
        Assert.Equal(ticks.Min, ticks.Values[0], 9);
        Assert.Equal(ticks.Max, ticks.Values[^1], 9);
    }

    [Fact]
    public void For_ValuesAreEvenlySpaced()
    {
        var ticks = AxisTicks.For(-56.5, 15, 6);

        Assert.Equal(new double[] { -60, -40, -20, 0, 20 }, ticks.Values);
        Assert.All(ticks.Values.Zip(ticks.Values.Skip(1)), pair =>
            Assert.Equal(ticks.Step, pair.Second - pair.First, 9));
    }

    [Fact]
    public void For_SwappedBounds_GivesSameTicks()
    {
        Assert.Equal(AxisTicks.For(0, 10000).Values, AxisTicks.For(10000, 0).Values);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-56.5)]
    public void For_ConstantValue_ExpandsRangeAroundIt(double value)
    {
        var ticks = AxisTicks.For(value, value);

        Assert.True(ticks.Min < value);
        Assert.True(ticks.Max > value);
        Assert.True(ticks.Values.Count >= 2);
    }

    [Theory]
    [InlineData(double.NaN, 1)]
    [InlineData(0, double.PositiveInfinity)]
    public void For_NotFiniteBounds_Throws(double min, double max)
    {
        Assert.Throws<ArgumentException>(() => AxisTicks.For(min, max));
    }

    [Fact]
    public void Format_ZeroTick_HasNoMinusSign()
    {
        var ticks = AxisTicks.For(-56.5, 15, 6);

        Assert.Equal("0", ticks.Format(ticks.Values[3]));
        Assert.Equal("0", ticks.Format(-0.0));
    }

    [Fact]
    public void Format_UsesSpaceAsThousandsSeparator()
    {
        var ticks = AxisTicks.For(0, 100000, 6);

        Assert.Equal("100 000", ticks.Format(100000));
    }

    [Fact]
    public void Format_ShowsAsManyDecimalsAsStepNeeds()
    {
        var ticks = AxisTicks.For(0, 0.04, 5);

        Assert.Equal("0.02", ticks.Format(0.02));
        Assert.Equal("-20", AxisTicks.For(-56.5, 15, 6).Format(-20));
    }
}
