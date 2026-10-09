namespace YnyrWASD.Core.Services.Mapping;

public static class AxisConverter
{
    // Promote to double before abs/negation so short.MinValue is safe.
    public static byte Convert(short value, double deadZone, bool invert = false)
    {
        if (!double.IsFinite(deadZone) || deadZone < 0 || deadZone >= 1)
            throw new ArgumentOutOfRangeException(nameof(deadZone));
        double normalized = value < 0 ? value / 32768.0 : value / 32767.0;
        double magnitude = Math.Abs(normalized);
        if (magnitude <= deadZone) return 128;
        normalized = Math.Sign(normalized) * (magnitude - deadZone) / (1 - deadZone);
        if (invert) normalized = -normalized;
        return (byte)Math.Clamp((int)Math.Round(128 + normalized * (normalized < 0 ? 128 : 127)), 0, 255);
    }
}
