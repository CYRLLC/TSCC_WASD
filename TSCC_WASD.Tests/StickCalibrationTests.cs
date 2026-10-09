using System.IO;
using Xunit;
using TSCC_WASD.Core.Services.Input;

namespace TSCC_WASD.Tests;

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
    public void AdaptiveCenterLearnsTheRestingOffset()
    {
        // Measured on a real NS2 Pro: right stick X rests at raw 2143-2146, not 2048.
        var adaptive = new AdaptiveStickCalibration();
        var calibration = adaptive.Observe(2145, 2064);
        Assert.InRange(calibration.X.Normalize(2145), -100, 100);
        Assert.InRange(calibration.Y.Normalize(2064), -100, 100);

        // Holding a real tilt for a long time must not drag the center toward it.
        for (int i = 0; i < 5000; i++) calibration = adaptive.Observe(2145 + 600, 2064);
        Assert.InRange(calibration.X.Center, 2140, 2150);
        // Full push from the learned center still saturates.
        calibration = adaptive.Observe(2145 + 1300, 2064);
        Assert.Equal(short.MaxValue, calibration.X.Normalize(2145 + 1300));
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
    public void CalibrationCacheKeepsUserAndFactorySeparate()
    {
        string path = Path.Combine(Path.GetTempPath(), "TSCC_WASD-tests", Guid.NewGuid().ToString("N"), "ns2.json");
        var cache = new Switch2CalibrationCache(path);
        Assert.False(cache.TryLoadFactory(out _, out _));
        Assert.False(cache.TryLoadUser(out _, out _));

        var factory = new Switch2StickCalibration(new(1990, 1350, 1400), new(2120, 1300, 1380));
        var user = new Switch2StickCalibration(new(2140, 1320, 1330), new(2060, 1310, 1290));
        cache.SaveUser(user, user);
        cache.SaveFactory(factory, factory); // Reading the factory data later must not erase the user's.
        Assert.True(cache.TryLoadUser(out var loadedUser, out _));
        Assert.Equal(user, loadedUser);
        Assert.True(cache.TryLoadFactory(out var loadedFactory, out _));
        Assert.Equal(factory, loadedFactory);

        cache.ClearUser();
        Assert.False(cache.TryLoadUser(out _, out _));
        Assert.True(cache.TryLoadFactory(out _, out _));

        File.WriteAllText(path, "{ broken");
        Assert.False(cache.TryLoadFactory(out _, out _));
    }

    [Fact]
    public void CalibratorUsesRestingCenterAndMeasuredTravel()
    {
        var calibrator = new StickCalibrator();
        for (int i = 0; !calibrator.CenterReady; i++)
            calibrator.AddCenterSample(new RawSticks(1985 + i % 3, 2123, 2144, 2064));
        Assert.False(calibrator.RangeReady);
        Assert.Throws<InvalidOperationException>(() => calibrator.Result());

        // Circle both sticks: left reaches ±1300, right reaches ±1250.
        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            calibrator.AddRangeSample(new RawSticks(1986 + dx * 1300, 2123 + dy * 1300, 2144 + dx * 1250, 2064 + dy * 1250));
        Assert.True(calibrator.RangeReady);

        var (left, right) = calibrator.Result();
        Assert.Equal(1986, left.X.Center);
        Assert.Equal(2144, right.X.Center);
        Assert.InRange(left.X.Normalize(1986), -50, 50);                 // At rest: zero.
        Assert.Equal(short.MaxValue, left.X.Normalize(1986 + 1300));      // Full push: full output.
        Assert.Equal(short.MinValue, right.Y.Normalize(2064 - 1250));
        Assert.InRange(left.X.Normalize(1986 + 650), 16500, 18000);       // Half push stays proportional.
    }

    [Fact]
    public void CalibratorRejectsTooShortTravel()
    {
        var calibrator = new StickCalibrator();
        while (!calibrator.CenterReady) calibrator.AddCenterSample(new RawSticks(2048, 2048, 2048, 2048));
        calibrator.AddRangeSample(new RawSticks(2048 + 300, 2048 + 300, 2048 + 300, 2048 + 300));
        Assert.False(calibrator.RangeReady);
        Assert.Equal(0.5, calibrator.Progress[0]);
        Assert.Equal(0.0, calibrator.Progress[1]);
    }
}
