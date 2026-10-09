using System.ComponentModel;
using System.Runtime.InteropServices;

namespace YnyrWASD.Core.Services.Input;

/// <summary>Reads the first available XInput slot (0–3) using the Windows inbox API.</summary>
public sealed class XInputReader : IInputReader
{
    [DllImport("xinput1_4.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint XInputGetState(uint dwUserIndex, out State state);

    public bool TryGetState(out State state)
    {
        for (uint index = 0; index < 4; index++)
        {
            uint result = XInputGetState(index, out state);
            if (result == 0) return true;
            if (result != 1167) throw new Win32Exception((int)result);
        }
        state = default;
        return false;
    }

    public void Dispose() { }
}
