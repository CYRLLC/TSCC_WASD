namespace TSCC_WASD.Core.Services.Input;

/// <summary>
/// Original Nintendo Switch Pro Controller (057E:2009) input reports and setup commands.
/// Layouts follow SDL's SDL_hidapi_switch.c (zlib) and dekuNukem's Nintendo_Switch_Reverse_Engineering
/// notes. Buttons map by position like the NS2 Pro: B (bottom)→A, A (right)→B, Y (left)→X, X (top)→Y,
/// Minus→Back, Plus→Start, Home→Guide, Capture→Touchpad.
/// </summary>
public static class SwitchProReport
{
    /// <summary>
    /// Full report 0x30 (12-bit sticks; sent once Steam or TSCC_WASD set it up) or the simple Bluetooth
    /// report 0x3F the controller sends by default. Full reports use <paramref name="left"/>/<paramref name="right"/>
    /// calibration; simple reports carry 16-bit sticks that need none.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> report, Switch2StickCalibration left, Switch2StickCalibration right,
        out State state, out RawSticks raw)
    {
        state = default;
        raw = default;
        if (report.Length >= 12 && report[0] == 0x30)
        {
            raw = new RawSticks(
                Switch2StickCalibration.UnpackX(report[6..]), Switch2StickCalibration.UnpackY(report[6..]),
                Switch2StickCalibration.UnpackX(report[9..]), Switch2StickCalibration.UnpackY(report[9..]));
            var buttons = GamepadButtonFlags.None;
            byte r = report[3], shared = report[4], l = report[5];
            if ((r & 0x01) != 0) buttons |= GamepadButtonFlags.X;             // Y (left)
            if ((r & 0x02) != 0) buttons |= GamepadButtonFlags.Y;             // X (top)
            if ((r & 0x04) != 0) buttons |= GamepadButtonFlags.A;             // B (bottom)
            if ((r & 0x08) != 0) buttons |= GamepadButtonFlags.B;             // A (right)
            if ((r & 0x40) != 0) buttons |= GamepadButtonFlags.RightShoulder; // R
            if ((shared & 0x01) != 0) buttons |= GamepadButtonFlags.Back;
            if ((shared & 0x02) != 0) buttons |= GamepadButtonFlags.Start;
            if ((shared & 0x04) != 0) buttons |= GamepadButtonFlags.RightThumb;
            if ((shared & 0x08) != 0) buttons |= GamepadButtonFlags.LeftThumb;
            if ((shared & 0x10) != 0) buttons |= GamepadButtonFlags.Guide;
            if ((shared & 0x20) != 0) buttons |= GamepadButtonFlags.Touchpad;
            if ((l & 0x01) != 0) buttons |= GamepadButtonFlags.DPadDown;
            if ((l & 0x02) != 0) buttons |= GamepadButtonFlags.DPadUp;
            if ((l & 0x04) != 0) buttons |= GamepadButtonFlags.DPadRight;
            if ((l & 0x08) != 0) buttons |= GamepadButtonFlags.DPadLeft;
            if ((l & 0x40) != 0) buttons |= GamepadButtonFlags.LeftShoulder;  // L
            state = new State
            {
                Gamepad = new Gamepad
                {
                    Buttons = buttons,
                    LeftTrigger = (byte)((l & 0x80) != 0 ? 255 : 0),  // ZL
                    RightTrigger = (byte)((r & 0x80) != 0 ? 255 : 0), // ZR
                    LeftThumbX = left.X.Normalize(raw.LeftX), LeftThumbY = left.Y.Normalize(raw.LeftY),
                    RightThumbX = right.X.Normalize(raw.RightX), RightThumbY = right.Y.Normalize(raw.RightY)
                }
            };
            return true;
        }
        if (report.Length >= 12 && report[0] == 0x3F)
        {
            var buttons = GamepadButtonFlags.None;
            byte b1 = report[1], b2 = report[2];
            if ((b1 & 0x01) != 0) buttons |= GamepadButtonFlags.A; // B (bottom)
            if ((b1 & 0x02) != 0) buttons |= GamepadButtonFlags.B; // A (right)
            if ((b1 & 0x04) != 0) buttons |= GamepadButtonFlags.X; // Y (left)
            if ((b1 & 0x08) != 0) buttons |= GamepadButtonFlags.Y; // X (top)
            if ((b1 & 0x10) != 0) buttons |= GamepadButtonFlags.LeftShoulder;
            if ((b1 & 0x20) != 0) buttons |= GamepadButtonFlags.RightShoulder;
            if ((b2 & 0x01) != 0) buttons |= GamepadButtonFlags.Back;
            if ((b2 & 0x02) != 0) buttons |= GamepadButtonFlags.Start;
            if ((b2 & 0x04) != 0) buttons |= GamepadButtonFlags.LeftThumb;
            if ((b2 & 0x08) != 0) buttons |= GamepadButtonFlags.RightThumb;
            if ((b2 & 0x10) != 0) buttons |= GamepadButtonFlags.Guide;
            if ((b2 & 0x20) != 0) buttons |= GamepadButtonFlags.Touchpad;
            buttons |= report[3] switch
            {
                0 => GamepadButtonFlags.DPadUp,
                1 => GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadRight,
                2 => GamepadButtonFlags.DPadRight,
                3 => GamepadButtonFlags.DPadDown | GamepadButtonFlags.DPadRight,
                4 => GamepadButtonFlags.DPadDown,
                5 => GamepadButtonFlags.DPadDown | GamepadButtonFlags.DPadLeft,
                6 => GamepadButtonFlags.DPadLeft,
                7 => GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadLeft,
                _ => GamepadButtonFlags.None
            };
            state = new State
            {
                Gamepad = new Gamepad
                {
                    Buttons = buttons,
                    LeftTrigger = (byte)((b1 & 0x40) != 0 ? 255 : 0),
                    RightTrigger = (byte)((b1 & 0x80) != 0 ? 255 : 0),
                    LeftThumbX = Simple(report, 4), LeftThumbY = Simple(report, 6, invert: true),
                    RightThumbX = Simple(report, 8), RightThumbY = Simple(report, 10, invert: true)
                }
            };
            return true;
        }
        return false;
    }

    /// <summary>Simple-report axis: unsigned 16-bit, centre 0x8000, Y grows downwards.</summary>
    private static short Simple(ReadOnlySpan<byte> report, int offset, bool invert = false)
    {
        int value = report[offset] | (report[offset + 1] << 8);
        int centred = invert ? Math.Min(32767, 32768 - value) : value - 32768;
        return (short)Math.Clamp(centred, short.MinValue, short.MaxValue);
    }

    /// <summary>USB-only commands (report 0x80) that make the controller talk HID over USB.</summary>
    public static readonly byte[][] UsbHandshake = [[0x80, 0x02], [0x80, 0x04]];

    /// <summary>Subcommand 0x03 0x30: switch to full reports. Rumble data is left neutral.</summary>
    public static byte[] SetFullReportMode(byte counter) => Subcommand(counter, 0x03, 0x30);

    /// <summary>Output report 0x01: counter, neutral rumble for both motors, then the subcommand.</summary>
    public static byte[] Subcommand(byte counter, byte id, byte argument)
    {
        var report = new byte[49];
        report[0] = 0x01;
        report[1] = (byte)(counter & 0x0F);
        byte[] neutralRumble = [0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40];
        neutralRumble.CopyTo(report, 2);
        report[10] = id;
        report[11] = argument;
        return report;
    }
}
