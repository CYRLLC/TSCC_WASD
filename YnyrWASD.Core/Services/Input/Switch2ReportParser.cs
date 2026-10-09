using System.Buffers.Binary;

namespace YnyrWASD.Core.Services.Input;

public readonly record struct Switch2AxisCalibration(int Center, int PositiveRange, int NegativeRange)
{
    public bool IsValid => Center is > 0 and < 4095 && PositiveRange is > 0 and <= 4095 && NegativeRange is > 0 and <= 4095;
    public short Normalize(int value)
    {
        if (!IsValid) throw new InvalidDataException("Invalid NS2 Pro stick calibration.");
        int delta = value - Center;
        double normalized = delta / (double)(delta < 0 ? NegativeRange : PositiveRange);
        return (short)Math.Clamp((int)Math.Round(normalized * (delta < 0 ? 32768 : 32767)), short.MinValue, short.MaxValue);
    }
}

public readonly record struct Switch2StickCalibration(Switch2AxisCalibration X, Switch2AxisCalibration Y)
{
    public static Switch2StickCalibration Nominal => new(new(2048, 2047, 2048), new(2048, 2047, 2048));
    public bool IsValid => X.IsValid && Y.IsValid;
    public static Switch2StickCalibration Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 9) throw new InvalidDataException("Incomplete NS2 Pro calibration.");
        return new(new(UnpackX(data), UnpackX(data[3..]), UnpackX(data[6..])),
            new(UnpackY(data), UnpackY(data[3..]), UnpackY(data[6..])));
    }
    internal static int UnpackX(ReadOnlySpan<byte> data) => data[0] | ((data[1] & 0x0F) << 8);
    internal static int UnpackY(ReadOnlySpan<byte> data) => (data[1] >> 4) | (data[2] << 4);
}

/// <summary>Decodes NS2 Pro USB report 0x05 into the existing positional controller state.</summary>
/// <remarks>Protocol reference: SDL_hidapi_switch2.c (SDL zlib license; see third-party notices).</remarks>
public static class Switch2ReportParser
{
    public static bool TryParse(ReadOnlySpan<byte> data, Switch2StickCalibration left,
        Switch2StickCalibration right, out State state)
    {
        state = default;
        if (data.Length != 64 || data[0] != 0x05 || !left.IsValid || !right.IsValid) return false;
        var buttons = GamepadButtonFlags.None;
        // Nintendo B/A/Y/X occupy the south/east/west/north positions.
        if ((data[5] & 0x04) != 0) buttons |= GamepadButtonFlags.A;
        if ((data[5] & 0x08) != 0) buttons |= GamepadButtonFlags.B;
        if ((data[5] & 0x01) != 0) buttons |= GamepadButtonFlags.X;
        if ((data[5] & 0x02) != 0) buttons |= GamepadButtonFlags.Y;
        if ((data[5] & 0x40) != 0) buttons |= GamepadButtonFlags.RightShoulder;
        if ((data[6] & 0x01) != 0) buttons |= GamepadButtonFlags.Back;
        if ((data[6] & 0x02) != 0) buttons |= GamepadButtonFlags.Start;
        if ((data[6] & 0x04) != 0) buttons |= GamepadButtonFlags.RightThumb;
        if ((data[6] & 0x08) != 0) buttons |= GamepadButtonFlags.LeftThumb;
        if ((data[7] & 0x01) != 0) buttons |= GamepadButtonFlags.DPadDown;
        if ((data[7] & 0x02) != 0) buttons |= GamepadButtonFlags.DPadUp;
        if ((data[7] & 0x04) != 0) buttons |= GamepadButtonFlags.DPadRight;
        if ((data[7] & 0x08) != 0) buttons |= GamepadButtonFlags.DPadLeft;
        if ((data[7] & 0x40) != 0) buttons |= GamepadButtonFlags.LeftShoulder;
        state = new State
        {
            PacketNumber = BinaryPrimitives.ReadUInt32LittleEndian(data[1..5]),
            Gamepad = new Gamepad
            {
                Buttons = buttons,
                LeftTrigger = (byte)((data[7] & 0x80) != 0 ? 255 : 0),
                RightTrigger = (byte)((data[5] & 0x80) != 0 ? 255 : 0),
                LeftThumbX = left.X.Normalize(Switch2StickCalibration.UnpackX(data[11..])),
                LeftThumbY = left.Y.Normalize(Switch2StickCalibration.UnpackY(data[11..])),
                RightThumbX = right.X.Normalize(Switch2StickCalibration.UnpackX(data[14..])),
                RightThumbY = right.Y.Normalize(Switch2StickCalibration.UnpackY(data[14..]))
            }
        };
        return true;
    }
}
