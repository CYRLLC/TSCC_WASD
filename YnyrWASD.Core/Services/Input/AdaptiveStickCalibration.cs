namespace YnyrWASD.Core.Services.Input;

/// <summary>
/// Stick calibration used when the factory calibration is unavailable (Steam owns the USB control
/// interface and none was remembered). It starts from a conservative range and widens each
/// direction to the furthest travel seen, so a fully pushed stick always reaches full output.
/// </summary>
/// <remarks>
/// The raw 12-bit range is ±2048, but real stick travel is much shorter; scaling by the nominal
/// range made a fully pushed stick read like a light push.
/// </remarks>
public sealed class AdaptiveStickCalibration
{
    /// <summary>Starting half-range in raw units; deliberately below typical travel so full push saturates.</summary>
    public const int InitialRange = 1000;
    /// <summary>The outermost part of the learned travel counts as full deflection.</summary>
    private const double OuterDeadZone = 0.05;
    private const int Center = 2048;
    private int _xPos = InitialRange, _xNeg = InitialRange, _yPos = InitialRange, _yNeg = InitialRange;

    public Switch2StickCalibration Current => new(
        new Switch2AxisCalibration(Center, Effective(_xPos), Effective(_xNeg)),
        new Switch2AxisCalibration(Center, Effective(_yPos), Effective(_yNeg)));

    /// <summary>Learns from one raw sample and returns the calibration to apply to it.</summary>
    public Switch2StickCalibration Observe(int rawX, int rawY)
    {
        Widen(rawX - Center, ref _xPos, ref _xNeg);
        Widen(rawY - Center, ref _yPos, ref _yNeg);
        return Current;
    }

    private static void Widen(int delta, ref int positive, ref int negative)
    {
        if (delta > positive) positive = Math.Min(delta, 2047);
        else if (-delta > negative) negative = Math.Min(-delta, 2048);
    }

    private static int Effective(int range) => Math.Max(1, (int)(range * (1 - OuterDeadZone)));
}
