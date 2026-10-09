using System.Runtime.InteropServices;

namespace YnyrWASD.Core.Services.Input;

/// <summary>Reads the first available XInput slot (0–3) using the Windows inbox API.</summary>
public sealed class XInputReader : IInputReader, IRumbleTarget
{
    [DllImport("xinput1_4.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint XInputGetState(uint dwUserIndex, out State state);

    // Undocumented but long-stable export that also reports the Guide button (0x0400).
    [DllImport("xinput1_4.dll", EntryPoint = "#100")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint XInputGetStateEx(uint dwUserIndex, out State state);

    [DllImport("xinput1_4.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint XInputSetState(uint dwUserIndex, ref Vibration vibration);

    [StructLayout(LayoutKind.Sequential)]
    private struct Vibration
    {
        public ushort LeftMotorSpeed;
        public ushort RightMotorSpeed;
    }

    private static bool _getStateExUnavailable;
    private volatile int _slot = -1;
    private int _rumble;

    public bool TryGetState(out State state)
    {
        for (uint index = 0; index < 4; index++)
        {
            uint result = GetState(index, out state);
            if (result == 0)
            {
                if (_slot != index) MoveRumble((int)index);
                return true;
            }
            // 1167 = not connected. Other codes are transient driver states; treat the slot as empty
            // so a long-running mapping keeps going instead of ending on one bad read.
        }
        _slot = -1;
        state = default;
        return false;
    }

    private static uint GetState(uint index, out State state)
    {
        if (!_getStateExUnavailable)
        {
            try { return XInputGetStateEx(index, out state); }
            catch (EntryPointNotFoundException) { _getStateExUnavailable = true; }
        }
        return XInputGetState(index, out state);
    }

    public void SetRumble(byte large, byte small)
    {
        Interlocked.Exchange(ref _rumble, (large << 8) | small);
        int slot = _slot;
        if (slot >= 0) Apply(slot, large, small);
    }

    private void MoveRumble(int index)
    {
        int previous = _slot;
        _slot = index;
        if (previous >= 0) Apply(previous, 0, 0);
        int value = Volatile.Read(ref _rumble);
        if (value != 0) Apply(index, (byte)(value >> 8), (byte)value);
    }

    private static void Apply(int slot, byte large, byte small)
    {
        var vibration = new Vibration { LeftMotorSpeed = (ushort)(large * 257), RightMotorSpeed = (ushort)(small * 257) };
        XInputSetState((uint)slot, ref vibration); // Best-effort; a missing pad simply ignores it.
    }

    public void Dispose()
    {
        int slot = _slot;
        if (slot >= 0) Apply(slot, 0, 0);
    }
}
