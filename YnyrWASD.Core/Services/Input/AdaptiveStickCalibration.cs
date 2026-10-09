namespace YnyrWASD.Core.Services.Input;

/// <summary>
/// Stick calibration used when the factory calibration is unavailable (Steam owns the USB control
/// interface and none was remembered). It learns the resting center and widens each direction to
/// the furthest travel seen, so the stick rests at zero and a full push reaches full output.
/// </summary>
/// <remarks>
/// The raw 12-bit range is ±2048, but real stick travel is much shorter; scaling by the nominal
/// range made a fully pushed stick read like a light push. A real stick also rests up to ~100 raw
/// units away from 2048, which matters once the range is realistic.
/// </remarks>
public sealed class AdaptiveStickCalibration
{
    /// <summary>Starting half-range in raw units; deliberately below typical travel so full push saturates.</summary>
    public const int InitialRange = 1000;
    /// <summary>The outermost part of the learned travel counts as full deflection.</summary>
    private const double OuterDeadZone = 0.05;
    private readonly Axis _x = new(), _y = new();

    public Switch2StickCalibration Current => new(_x.Calibration, _y.Calibration);

    /// <summary>Learns from one raw sample and returns the calibration to apply to it.</summary>
    public Switch2StickCalibration Observe(int rawX, int rawY)
    {
        _x.Observe(rawX);
        _y.Observe(rawY);
        return Current;
    }

    private sealed class Axis
    {
        private const double NominalCenter = 2048;
        /// <summary>A first sample this close to 2048 is taken as the stick at rest.</summary>
        private const int RestWindow = 200;
        /// <summary>Only samples this close to the center refine it, so held tilts don't drag it.</summary>
        private const int RefineWindow = 100;
        private const double RefineRate = 0.02;
        private double _center = NominalCenter;
        private bool _seen;
        private int _positive = InitialRange, _negative = InitialRange;

        public Switch2AxisCalibration Calibration => new((int)Math.Round(_center), Effective(_positive), Effective(_negative));

        public void Observe(int raw)
        {
            if (!_seen)
            {
                _seen = true;
                if (Math.Abs(raw - NominalCenter) <= RestWindow) _center = raw;
            }
            else if (Math.Abs(raw - _center) <= RefineWindow)
            {
                _center += (raw - _center) * RefineRate;
            }
            int delta = raw - (int)Math.Round(_center);
            int center = (int)Math.Round(_center);
            if (delta > _positive) _positive = Math.Min(delta, 4095 - center);
            else if (-delta > _negative) _negative = Math.Min(-delta, center);
        }

        private static int Effective(int range) => Math.Max(1, (int)(range * (1 - OuterDeadZone)));
    }
}
