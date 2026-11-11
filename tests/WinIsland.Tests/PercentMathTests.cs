using WinIsland.Services;
using Xunit;

namespace WinIsland.Tests;

public sealed class PercentMathTests
{
    [Theory]
    [InlineData(-20, 0)]
    [InlineData(0, 0)]
    [InlineData(23.2, 23)]
    [InlineData(23.8, 24)]
    [InlineData(100, 100)]
    [InlineData(145, 100)]
    public void ClampPercent_ClampsAndRounds(double input, int expected)
    {
        Assert.Equal(expected, PercentMath.ClampPercent(input));
    }

    [Fact]
    public void ClampPercent_ReturnsZeroForNonFiniteValues()
    {
        Assert.Equal(0, PercentMath.ClampPercent(double.NaN));
        Assert.Equal(0, PercentMath.ClampPercent(double.PositiveInfinity));
        Assert.Equal(0, PercentMath.ClampPercent(double.NegativeInfinity));
    }

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(50, 0.5f)]
    [InlineData(100, 1f)]
    [InlineData(120, 1f)]
    public void ToScalar_ReturnsNormalizedValues(double input, float expected)
    {
        Assert.Equal(expected, PercentMath.ToScalar(input));
    }
}
