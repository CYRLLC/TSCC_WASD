using System.Runtime.InteropServices;

namespace YnyrWASD.Core.Services.Input;

internal static class WindowsDevicePaths
{
    internal static readonly Guid Hid = new("4d1e55b2-f16f-11cf-88cb-001111000030");
    // DeviceInterfaceGUID advertised by Switch 2 Pro's Microsoft OS descriptors.
    internal static readonly Guid Switch2Usb = new("6f13725e-ef0e-4fd3-ae5f-b2de989ec825");

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_Device_Interface_List_SizeW(out uint length, ref Guid guid, string? device, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_Device_Interface_ListW(ref Guid guid, string? device, [Out] char[] buffer, uint length, uint flags);

    internal static string[] Find(Guid guid)
    {
        for (int retry = 0; retry < 3; retry++)
        {
            uint result = CM_Get_Device_Interface_List_SizeW(out uint length, ref guid, null, 0);
            if (result != 0) throw new IOException($"Device enumeration failed: CONFIGRET 0x{result:X}.");
            var buffer = new char[length];
            result = CM_Get_Device_Interface_ListW(ref guid, null, buffer, length, 0);
            if (result == 0) return new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Contains("vid_057e&pid_2069", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (result != 0x1A) throw new IOException($"Device enumeration failed: CONFIGRET 0x{result:X}.");
        }
        return [];
    }
}
