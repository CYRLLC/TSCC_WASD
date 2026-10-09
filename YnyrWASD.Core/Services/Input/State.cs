using System.Runtime.InteropServices;

namespace YnyrWASD.Core.Services.Input;

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
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000
}
