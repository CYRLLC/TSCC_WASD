using System.IO;
using Xunit;
using YnyrWASD.Core.Services.Input;

namespace YnyrWASD.Tests;

public class StickCalibrationTests
{
    [Fact]
    public void NominalRangeLeavesARealisticFullPushFarFromFullOutput()
    {
        // The regression: ~1300 raw units of travel under the ±2048 nominal range is only ~63%.
        short value = Switch2StickCalibration.Nominal.X.Normalize(2048 + 1300);
        Assert.InRange(value, 20000, 21000);
    }

    [Fact]
    public void AdaptiveRangeSaturatesAtConservativeStartThenLearnsLongerTravel()
    {
        var adaptive = new AdaptiveStickCalibration();
        // Before learning, the conservative start already reaches full output.
        var calibration = adaptive.Observe(2048 + AdaptiveStickCalibration.InitialRange, 2048);
        Assert.Equal(short.MaxValue, calibration.X.Normalize(2048 + AdaptiveStickCalibration.InitialRange));

        // A longer push widens that direction only; full push is still full output.
        calibration = adaptive.Observe(2048 + 1400, 2048 - 1350);
        Assert.Equal(short.MaxValue, calibration.X.Normalize(2048 + 1400));
        Assert.Equal(short.MinValue, calibration.Y.Normalize(2048 - 1350));
        // Half of the learned travel stays proportional instead of saturating.
        Assert.InRange(calibration.X.Normalize(2048 + 700), 16000, 18000);
        // The untouched direction keeps the conservative start.
        Assert.Equal(short.MinValue, calibration.X.Normalize(2048 - AdaptiveStickCalibration.InitialRange));
    }

    [Fact]
    public void AdaptiveRangeStaysValidAtRawExtremes()
    {
        var calibration = new AdaptiveStickCalibration().Observe(4095, 0);
        Assert.True(calibration.IsValid);
        Assert.Equal(short.MaxValue, calibration.X.Normalize(4095));
        Assert.Equal(short.MinValue, calibration.Y.Normalize(0));
    }

    [Fact]
    public void CalibrationCacheRoundTripsAndRejectsInvalidData()
    {
        string path = Path.Combine(Path.GetTempPath(), "YnyrWASD-tests", Guid.NewGuid().ToString("N"), "ns2.json");
        var cache = new Switch2CalibrationCache(path);
        Assert.False(cache.TryLoad(out _, out _));

        var left = new Switch2StickCalibration(new(1990, 1350, 1400), new(2120, 1300, 1380));
        var right = new Switch2StickCalibration(new(2140, 1320, 1330), new(2060, 1310, 1290));
        cache.Save(left, right);
        Assert.True(cache.TryLoad(out var loadedLeft, out var loadedRight));
        Assert.Equal(left, loadedLeft);
        Assert.Equal(right, loadedRight);

        File.WriteAllText(path, "{ broken");
        Assert.False(cache.TryLoad(out _, out _));
    }
}
