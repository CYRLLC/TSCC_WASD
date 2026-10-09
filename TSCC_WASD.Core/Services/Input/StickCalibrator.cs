namespace TSCC_WASD.Core.Services.Input;

/// <summary>Raw 12-bit NS2 Pro stick positions, before any calibration.</summary>
public readonly record struct RawSticks(int LeftX, int LeftY, int RightX, int RightY);

/// <summary>
/// Calibration wizard logic: average the resting position, then record the furthest travel in
/// every direction while the user circles both sticks at the edge.
/// </summary>
public sealed class StickCalibrator
{
    /// <summary>Each direction must travel at least this far (raw units) before the result is accepted.</summary>
    public const int MinimumTravel = 600;
    /// <summary>The outermost part of the measured travel still counts as full deflection.</summary>
    private const double OuterDeadZone = 0.05;
    private const int CenterSamplesNeeded = 50;

    private readonly long[] _centerSum = new long[4];
    private int _centerSamples;
    private int[] _center = [2048, 2048, 2048, 2048];
    private readonly int[] _positive = new int[4], _negative = new int[4];

    public bool CenterReady => _centerSamples >= CenterSamplesNeeded;

    /// <summary>Step 1: feed samples while the sticks are released.</summary>
    public void AddCenterSample(RawSticks raw)
    {
        int[] values = Values(raw);
        for (int i = 0; i < 4; i++) _centerSum[i] += values[i];
        _centerSamples++;
        _center = [.. _centerSum.Select(sum => (int)Math.Round(sum / (double)_centerSamples))];
    }

    /// <summary>Step 2: feed samples while the sticks circle the edge.</summary>
    public void AddRangeSample(RawSticks raw)
    {
        int[] values = Values(raw);
        for (int i = 0; i < 4; i++)
        {
            int delta = values[i] - _center[i];
            if (delta > _positive[i]) _positive[i] = delta;
            else if (-delta > _negative[i]) _negative[i] = -delta;
        }
    }

    /// <summary>
    /// Progress per direction, 0–1, in the order left +X, −X, +Y, −Y, then right +X, −X, +Y, −Y.
    /// </summary>
    public IReadOnlyList<double> Progress =>
        [.. Enumerable.Range(0, 4).SelectMany(i => new[] { _positive[i], _negative[i] })
            .Select(travel => Math.Min(1.0, travel / (double)MinimumTravel))];

    public bool RangeReady => Progress.All(p => p >= 1.0);

    public (Switch2StickCalibration Left, Switch2StickCalibration Right) Result()
    {
        if (!CenterReady || !RangeReady) throw new InvalidOperationException("Calibration is incomplete.");
        Switch2AxisCalibration Axis(int i) => new(_center[i], Effective(_positive[i]), Effective(_negative[i]));
        return (new(Axis(0), Axis(1)), new(Axis(2), Axis(3)));
    }

    private static int Effective(int travel) => Math.Max(1, (int)(travel * (1 - OuterDeadZone)));
    private static int[] Values(RawSticks raw) => [raw.LeftX, raw.LeftY, raw.RightX, raw.RightY];
}
