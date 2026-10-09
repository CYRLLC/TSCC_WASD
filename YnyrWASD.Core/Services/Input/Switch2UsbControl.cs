using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace YnyrWASD.Core.Services.Input;

/// <summary>Uses the inbox WinUSB driver on NS2 Pro interface 1. No driver installation or flash writes.</summary>
internal sealed class Switch2UsbControl : IDisposable
{
    private readonly SafeFileHandle _file;
    private IntPtr _usb;
    private byte _input, _output;

    internal Switch2UsbControl(string path)
    {
        _file = CreateFileW(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
        try
        {
            if (_file.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            Check(WinUsb_Initialize(_file, out _usb));
            Check(WinUsb_QueryInterfaceSettings(_usb, 0, out var descriptor));
            for (byte i = 0; i < descriptor.NumEndpoints; i++)
            {
                Check(WinUsb_QueryPipe(_usb, 0, i, out var pipe));
                if (pipe.PipeType != 2) continue;
                if ((pipe.PipeId & 0x80) != 0) _input = pipe.PipeId;
                else _output = pipe.PipeId;
            }
            if (_input == 0 || _output == 0) throw new IOException("NS2 Pro bulk endpoints unavailable.");
            uint timeout = 200;
            Check(WinUsb_SetPipePolicy(_usb, _input, 3, 4, ref timeout));
            Check(WinUsb_SetPipePolicy(_usb, _output, 3, 4, ref timeout));
        }
        catch { Dispose(); throw; }
    }

    internal byte[] Exchange(byte[] request, int maxReply = 64)
    {
        Check(WinUsb_WritePipe(_usb, _output, request, (uint)request.Length, out uint sent, IntPtr.Zero));
        if (sent != request.Length) throw new IOException("Incomplete NS2 Pro USB request.");
        var response = new List<byte>();
        while (response.Count < maxReply)
        {
            byte[] part = new byte[Math.Min(64, maxReply - response.Count)];
            Check(WinUsb_ReadPipe(_usb, _input, part, (uint)part.Length, out uint received, IntPtr.Zero));
            response.AddRange(part.AsSpan(0, (int)received).ToArray());
            if (received < part.Length) break;
        }
        return response.ToArray();
    }

    internal byte[] ReadFlash(uint address)
    {
        byte[] request = [0x02, 0x91, 0, 0x01, 0, 0x08, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(12), address);
        var reply = Exchange(request, 80);
        if (reply.Length < 80 || reply[0] != 0x02) throw new IOException("Invalid NS2 Pro calibration reply.");
        return reply[16..80];
    }

    private static void Check(bool success)
    {
        if (!success) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Dispose()
    {
        if (_usb != IntPtr.Zero) { WinUsb_Free(_usb); _usb = IntPtr.Zero; }
        _file.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct InterfaceDescriptor
    {
        public byte Length, DescriptorType, InterfaceNumber, AlternateSetting, NumEndpoints, InterfaceClass,
            InterfaceSubClass, InterfaceProtocol, InterfaceString;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PipeInformation
    {
        public int PipeType;
        public byte PipeId;
        public ushort MaximumPacketSize;
        public byte Interval;
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_Initialize(SafeFileHandle file, out IntPtr handle);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_Free(IntPtr handle);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_QueryInterfaceSettings(IntPtr handle, byte alternate, out InterfaceDescriptor descriptor);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_QueryPipe(IntPtr handle, byte alternate, byte index, out PipeInformation pipe);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_SetPipePolicy(IntPtr handle, byte pipe, uint policy, uint length, ref uint value);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_WritePipe(IntPtr handle, byte pipe, byte[] buffer, uint length, out uint transferred, IntPtr overlapped);
    [DllImport("winusb.dll", SetLastError = true)]
    private static extern bool WinUsb_ReadPipe(IntPtr handle, byte pipe, [Out] byte[] buffer, uint length, out uint transferred, IntPtr overlapped);
}
