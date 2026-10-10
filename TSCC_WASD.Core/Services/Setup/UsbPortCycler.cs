using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace TSCC_WASD.Core.Services.Setup;

/// <summary>One node on the way from a controller up to the device tree root.</summary>
public sealed record DeviceNode(string Id, string? Service);

/// <summary>A USB hub port to power-cycle, found from a controller's ancestry.</summary>
public sealed record UsbPort(string HubId, uint Port, string DeviceId);

/// <summary>
/// Makes Windows re-enumerate connected controllers by power-cycling their USB hub port. Unlike
/// disabling or restarting the device, a port cycle is a real unplug that a program holding the
/// controller open (Steam) cannot veto. Once the controller comes back, HidHide is already hiding it,
/// so only TSCC_WASD can open it again. Requires administrator rights; Bluetooth controllers are
/// only counted, because they have no port to cycle.
/// </summary>
[SupportedOSPlatform("windows")]
public static class UsbPortCycler
{
    private static readonly Guid UsbHubInterface = new("f18a0e88-c30c-11d0-8815-00a0c906bed8");
    private const uint IoctlUsbHubCyclePort = 0x220444; // CTL_CODE(FILE_DEVICE_USB, 273, METHOD_BUFFERED, FILE_ANY_ACCESS)
    private const uint CmDrpService = 0x05, CmDrpAddress = 0x1D;
    // Xbox Wireless Adapters: cycling one reconnects every controller paired with it.
    private static readonly string[] WirelessAdapters = ["VID_045E&PID_02E6", "VID_045E&PID_02FE"];

    public sealed record Plan(IReadOnlyList<UsbPort> Ports, int Bluetooth, bool WirelessAdapter);

    /// <summary>The child directly below the first USB hub on the chain, which is what sits on the port.</summary>
    public static int? HubChildIndex(IReadOnlyList<DeviceNode> chain)
    {
        for (int i = 1; i < chain.Count; i++)
            if (chain[i].Service?.StartsWith("USBHUB", StringComparison.OrdinalIgnoreCase) == true) return i - 1;
        return null;
    }

    public static bool IsBluetooth(IEnumerable<DeviceNode> chain) => chain.Any(node =>
        node.Id.StartsWith(@"BTHENUM\", StringComparison.OrdinalIgnoreCase)
        || node.Id.StartsWith(@"BTHLEDEVICE\", StringComparison.OrdinalIgnoreCase)
        || node.Id.StartsWith(@"BTHLE\", StringComparison.OrdinalIgnoreCase));

    /// <summary>Connected NS2 Pro and Xbox controllers grouped into USB ports to cycle and Bluetooth ones.</summary>
    public static Plan PlanConnected()
    {
        var ports = new Dictionary<(string, uint), UsbPort>();
        int bluetooth = 0;
        bool adapter = false;
        var seenBluetooth = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in Input.WindowsDevicePaths.FindPhysicalControllers())
        {
            var chain = Ancestry(id);
            if (chain.Count == 0) continue; // Remembered but not connected.
            if (IsBluetooth(chain))
            {
                // One controller exposes several interfaces; count it once by its Bluetooth node.
                var node = chain.First(n => n.Id.StartsWith("BTH", StringComparison.OrdinalIgnoreCase));
                if (seenBluetooth.Add(node.Id)) bluetooth++;
                continue;
            }
            if (HubChildIndex(chain) is not { } index) continue;
            var child = chain[index];
            if (Address(child.Id) is not { } port) continue;
            ports.TryAdd((chain[index + 1].Id, port), new UsbPort(chain[index + 1].Id, port, child.Id));
            adapter |= WirelessAdapters.Any(a => child.Id.Contains(a, StringComparison.OrdinalIgnoreCase));
        }
        return new Plan([.. ports.Values], bluetooth, adapter);
    }

    /// <summary>Power-cycles one hub port. Throws when it fails (for example without administrator rights).</summary>
    public static void Cycle(UsbPort port)
    {
        string hubPath = HubInterfacePath(port.HubId)
            ?? throw new IOException($"No USB hub interface for {port.HubId}.");
        using var hub = CreateFileW(hubPath, 0xC0000000 /* GENERIC_READ | GENERIC_WRITE */, 3 /* share read+write */,
            IntPtr.Zero, 3 /* OPEN_EXISTING */, 0, IntPtr.Zero);
        if (hub.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        var parameters = new CyclePortParams { ConnectionIndex = port.Port };
        int size = Marshal.SizeOf<CyclePortParams>();
        if (!DeviceIoControl(hub, IoctlUsbHubCyclePort, ref parameters, size, ref parameters, size, out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (parameters.StatusReturned != 0)
            throw new IOException($"The hub refused to cycle port {port.Port} (status 0x{parameters.StatusReturned:X}).");
    }

    /// <summary>The device and its parents up to the root; empty when the device is not connected.</summary>
    private static List<DeviceNode> Ancestry(string deviceId)
    {
        var chain = new List<DeviceNode>();
        if (CM_Locate_DevNodeW(out uint node, deviceId, 0) != 0) return chain; // 0 = present devices only.
        for (int depth = 0; depth < 16; depth++)
        {
            string? id = DeviceId(node);
            if (id is null) break;
            chain.Add(new DeviceNode(id, StringProperty(node, CmDrpService)));
            if (CM_Get_Parent(out uint parent, node, 0) != 0) break;
            node = parent;
        }
        return chain;
    }

    private static uint? Address(string deviceId)
    {
        if (CM_Locate_DevNodeW(out uint node, deviceId, 0) != 0) return null;
        var buffer = new byte[4];
        uint length = 4;
        return CM_Get_DevNode_Registry_PropertyW(node, CmDrpAddress, out _, buffer, ref length, 0) == 0
            ? BitConverter.ToUInt32(buffer) : null;
    }

    private static string? DeviceId(uint node)
    {
        var buffer = new char[400];
        return CM_Get_Device_IDW(node, buffer, (uint)buffer.Length, 0) == 0 ? new string(buffer).TrimEnd('\0') : null;
    }

    private static string? StringProperty(uint node, uint property)
    {
        var buffer = new byte[512];
        uint length = (uint)buffer.Length;
        if (CM_Get_DevNode_Registry_PropertyW(node, property, out _, buffer, ref length, 0) != 0) return null;
        return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)length).TrimEnd('\0');
    }

    private static string? HubInterfacePath(string hubId)
    {
        var guid = UsbHubInterface;
        if (CM_Get_Device_Interface_List_SizeW(out uint length, ref guid, hubId, 0) != 0 || length <= 1) return null;
        var buffer = new char[length];
        if (CM_Get_Device_Interface_ListW(ref guid, hubId, buffer, length, 0) != 0) return null;
        return new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CyclePortParams
    {
        public uint ConnectionIndex;
        public uint StatusReturned;
    }

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);
    [DllImport("cfgmgr32.dll", ExactSpelling = true)]
    private static extern uint CM_Get_Parent(out uint parent, uint devInst, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_Device_IDW(uint devInst, [Out] char[] buffer, uint length, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_DevNode_Registry_PropertyW(uint devInst, uint property, out uint type,
        [Out] byte[] buffer, ref uint length, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_Device_Interface_List_SizeW(out uint length, ref Guid guid, string? device, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_Device_Interface_ListW(ref Guid guid, string? device, [Out] char[] buffer, uint length, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security,
        uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint code, ref CyclePortParams input, int inputSize,
        ref CyclePortParams output, int outputSize, out int returned, IntPtr overlapped);
}
