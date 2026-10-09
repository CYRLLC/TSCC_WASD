using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YnyrWASD.Core.Services.Input;

/// <summary>One NS2 Pro USB device. Background I/O never blocks the mapping poll loop.</summary>
public sealed class Switch2InputReader : IInputReader
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private State _latest;
    private long _receivedAt;
    private bool _hasState;
    private string _status = "尋找 NS2 Pro（USB）...";
    private bool _disposed;

    public Switch2InputReader() => _worker = Task.Run(() => RunAsync(_stop.Token));
    public string Status { get { lock (_gate) return _status; } }

    public bool TryGetState(out State state)
    {
        lock (_gate)
        {
            bool fresh = _hasState && Stopwatch.GetElapsedTime(_receivedAt) < TimeSpan.FromMilliseconds(250);
            state = fresh ? _latest : default;
            return fresh;
        }
    }

    private void SetStatus(string status, bool clear = true)
    {
        lock (_gate)
        {
            _status = status;
            if (clear) { _hasState = false; _latest = default; }
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var paths = WindowsDevicePaths.Find(WindowsDevicePaths.Switch2Usb);
                    if (paths.Length > 1) throw new IOException("目前一次支援一支 NS2 Pro，請拔除其他 NS2 Pro。");
                    if (paths.Length == 0) throw new IOException("等待 NS2 Pro USB 連線（不支援藍牙）。");
                    SetStatus("初始化 NS2 Pro USB 與搖桿校準...");
                    (Switch2StickCalibration Left, Switch2StickCalibration Right) calibration;
                    bool sharedMode = false;
                    try
                    {
                        using var usb = new Switch2UsbControl(paths[0]);
                        calibration = Switch2Protocol.Initialize(usb, token);
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 5 or 32)
                    {
                        // Steam can own USB control while HID reports remain shareable.
                        // Do not fight ownership or install a different driver.
                        sharedMode = true;
                        calibration = (Switch2StickCalibration.Nominal, Switch2StickCalibration.Nominal);
                        SetStatus("NS2 Pro HID 共用模式：等待 Steam 初始化（使用預設校準）。");
                    }

                    var hidPaths = WindowsDevicePaths.Find(WindowsDevicePaths.Hid);
                    if (hidPaths.Length != 1) throw new IOException("找不到唯一的 NS2 Pro HID 介面；請檢查 HidHide 允許路徑。");
                    using var handle = Switch2UsbControl.CreateFileW(hidPaths[0], 0x80000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                    if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                    using var stream = new FileStream(handle, FileAccess.Read, 64, isAsync: true);
                    byte[] report = new byte[64];
                    while (!token.IsCancellationRequested)
                    {
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                        timeout.CancelAfter(TimeSpan.FromSeconds(2));
                        int length = await stream.ReadAsync(report, timeout.Token).ConfigureAwait(false);
                        if (length == 0) throw new IOException("NS2 Pro 已斷線。");
                        if (!Switch2ReportParser.TryParse(report.AsSpan(0, length), calibration.Left, calibration.Right, out var state))
                            continue;
                        lock (_gate)
                        {
                            _latest = state;
                            _receivedAt = Stopwatch.GetTimestamp();
                            _hasState = true;
                            _status = sharedMode ? "NS2 Pro（HID 共用，預設校準）" : "NS2 Pro（USB，原廠／使用者校準）";
                        }
                    }
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                { SetStatus("NS2 Pro 無輸入回報，正在重新連線..."); }
                catch (Win32Exception ex) when (ex.NativeErrorCode is 5 or 32)
                { SetStatus("NS2 Pro USB 被占用：請先完整退出 Steam／其他手把工具，再重新啟動映射。"); }
                catch (Exception ex) when (ex is IOException or Win32Exception)
                { SetStatus($"NS2 Pro：{ex.Message}"); }
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { SetStatus($"NS2 Pro 輸入已停止：{ex.Message}"); }
        finally { lock (_gate) { _hasState = false; _latest = default; } }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Cancel();
        _worker.GetAwaiter().GetResult();
        _stop.Dispose();
    }
}
