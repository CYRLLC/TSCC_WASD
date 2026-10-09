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
        if ((data[6] & 0x10) != 0) buttons |= GamepadButtonFlags.Guide;    // Home → PS
        if ((data[6] & 0x20) != 0) buttons |= GamepadButtonFlags.Touchpad; // Capture → touchpad click
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

    /// <summary>
    /// Reads the IMU block of report 0x05 and converts it to DualShock 4 raw axes.
    /// Returns false when the controller is not streaming sensor data (zero sensor timestamp).
    /// </summary>
    public static bool TryParseMotion(ReadOnlySpan<byte> data, out MotionSample motion)
    {
        motion = default;
        if (data.Length != 64 || data[0] != 0x05) return false;
        if (BinaryPrimitives.ReadUInt32LittleEndian(data[0x2b..]) == 0) return false;
        // Axis order and signs follow SDL's conversion to the DS4-compatible convention.
        // NS2 gyro full scale is about ±2000 deg/s like DS4, so raw values carry over;
        // NS2 accel is ±8 g (4096 LSB/g) versus DS4's 8192 LSB/g.
        static short Accel(int value) => (short)Math.Clamp(value * 2, short.MinValue, short.MaxValue);
        static short Negate(short value) => value == short.MinValue ? short.MaxValue : (short)-value;
        motion = new MotionSample(
            GyroX: Raw(data, 0x37), GyroY: Raw(data, 0x3b), GyroZ: Negate(Raw(data, 0x39)),
            AccelX: Accel(Raw(data, 0x31)), AccelY: Accel(Raw(data, 0x35)), AccelZ: Accel(-Raw(data, 0x33)));
        return true;
    }

    private static short Raw(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadInt16LittleEndian(data[offset..]);
}

/// <summary>NS2 Pro HD rumble output report. Encoding adapted from SDL_hidapi_switch2.c (zlib).</summary>
public static class Switch2Rumble
{
    private const ushort HighFrequency = 0x187, LowFrequency = 0x112;
    private const int MaxAmplitude = 29000; // SDL clamps to protect the actuators.

    /// <param name="large">Low-frequency motor, 0–255.</param>
    /// <param name="small">High-frequency motor, 0–255.</param>
    public static byte[] BuildReport(byte large, byte small, int sequence)
    {
        var report = new byte[64];
        ushort lowAmp = (ushort)(large * 257 * MaxAmplitude / ushort.MaxValue);
        ushort highAmp = (ushort)(small * 257 * MaxAmplitude / ushort.MaxValue);
        report[0] = 0x02;
        report[1] = (byte)(0x50 | (sequence & 0x0F));
        report[2] = (byte)(HighFrequency & 0xFF);
        report[3] = (byte)(((highAmp >> 4) & 0xFC) | ((HighFrequency >> 8) & 0x03));
        report[4] = (byte)((highAmp >> 12) | (LowFrequency << 4));
        report[5] = (byte)((lowAmp & 0xC0) | ((LowFrequency >> 4) & 0x3F));
        report[6] = (byte)(lowAmp >> 8);
        // The Pro controller has two actuators; SDL drives both with the same packet.
        report.AsSpan(1, 6).CopyTo(report.AsSpan(0x11));
        return report;
    }
}
