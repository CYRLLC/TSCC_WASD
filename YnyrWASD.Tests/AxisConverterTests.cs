using Xunit;
using YnyrWASD.Core.Services.Mapping;

namespace YnyrWASD.Tests;

public class AxisConverterTests
{
    [Theory]
    [InlineData(short.MinValue, false, 0)]
    [InlineData(short.MaxValue, false, 255)]
    [InlineData(short.MinValue, true, 255)]
    [InlineData(short.MaxValue, true, 0)]
    [InlineData(0, false, 128)]
    [InlineData(0, true, 128)]
    [InlineData(1000, false, 128)]
    [InlineData(-1000, true, 128)]
    public void EndpointsAndDeadZone(short input, bool invert, byte expected)
        => Assert.Equal(expected, AxisConverter.Convert(input, 0.08, invert));

    [Fact]
    public void EveryShortInputIsMonotonicAndSafe()
    {
        byte previous = 0;
        for (int value = short.MinValue; value <= short.MaxValue; value++)
        {
            byte actual = AxisConverter.Convert((short)value, 0.08);
            Assert.True(actual >= previous, $"Not monotonic at {value}");
            previous = actual;
        }
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidDeadZoneRejected(double value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => AxisConverter.Convert(0, value));
}
