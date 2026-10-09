using System.Runtime.InteropServices;

namespace TSCC_WASD.Core.Services.Input;

// Binary layout matches XINPUT_STATE / XINPUT_GAMEPAD in the Windows SDK.
[StructLayout(LayoutKind.Sequential)]
public struct State
{
    public uint PacketNumber;
    public Gamepad Gamepad;
}

[StructLayout(LayoutKind.Sequential)]
public struct Gamepad
{
    public GamepadButtonFlags Buttons;
    public byte LeftTrigger;
    public byte RightTrigger;
    public short LeftThumbX;
    public short LeftThumbY;
    public short RightThumbX;
    public short RightThumbY;
}

[Flags]
public enum GamepadButtonFlags : ushort
{
    None = 0,
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    // XInputGetStateEx reports Guide here; Touchpad uses the otherwise unused bit for NS2 Capture.
    Guide = 0x0400,
    Touchpad = 0x0800,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000
}

/// <summary>Motion sample already scaled to DualShock 4 raw units (gyro 16 LSB per deg/s, accel 8192 LSB per g).</summary>
public readonly record struct MotionSample(short GyroX, short GyroY, short GyroZ, short AccelX, short AccelY, short AccelZ);
