using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services.Input;
using YnyrWASD.Core.Services.VirtualControllers;

namespace YnyrWASD.Core.Services.Mapping;

/// <summary>Owns one input/output pair. Lifecycle methods are called serially by the coordinator.</summary>
public sealed class MappingSession : IAsyncDisposable
{
    private readonly MappingProfile _profile;
    private readonly IInputReader _input;
    private readonly IVirtualController _output;
    private readonly Action<string>? _statusCallback;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;
    private int _inputReleased;
    private volatile string _inputSummary = "尚未收到輸入";

    public MappingSession(MappingProfile profile, IInputReader input, IVirtualController output,
        Action<string>? statusCallback = null)
    {
        _profile = profile.Snapshot();
        _input = input;
        _output = output;
        _statusCallback = statusCallback;
    }

    private static readonly TimeSpan OutputRetryDelay = TimeSpan.FromSeconds(1);
    public bool IsRunning => _loopTask is { IsCompleted: false };
    public string? LastError { get; private set; }
    public string InputSummary => _inputSummary;

    public bool TryStart()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_cts is not null) throw new InvalidOperationException("A session can only be started once.");
        if (!_output.TryConnect(out var error))
        {
            LastError = error ?? "無法建立虛擬手把。";
            return false;
        }
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunAsync(_cts.Token));
        return true;
    }

    private async Task RunAsync(CancellationToken token)
    {
        string? previousStatus = null;
        try
        {
            // Runs until stopped: input loss sends neutral, driver errors reconnect the virtual pad.
            while (!token.IsCancellationRequested)
            {
                bool available;
                State state;
                try { available = _input.TryGetState(out state); }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    available = false;
                    state = default;
                }
                _inputSummary = available
                    ? $"按鍵：{state.Gamepad.Buttons} · 左搖桿 ({state.Gamepad.LeftThumbX}, {state.Gamepad.LeftThumbY}) · 右搖桿 ({state.Gamepad.RightThumbX}, {state.Gamepad.RightThumbY}) · L2/R2 {state.Gamepad.LeftTrigger}/{state.Gamepad.RightTrigger}"
                    : "尚未收到有效輸入（輸出歸零）";
                string status;
                try
                {
                    if (!_output.IsConnected && !_output.TryConnect(out var connectError))
                        throw new InvalidOperationException(connectError ?? "無法重新建立虛擬手把。");
                    // Always send neutral input on disconnect; never leave buttons held.
                    _output.PushState(available ? state : default, _profile.DeadZone);
                    status = available ? $"映射運作中：{_input.Status} → DualShock 4"
                        : $"等待輸入（虛擬 DS4 保持連線，輸出已歸零）：{_input.Status}";
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    try { _output.Disconnect(); }
                    catch { /* Reconnect will be attempted on the next pass. */ }
                    status = $"虛擬 DS4 發生錯誤，稍後自動重新連線：{ex.Message}";
                    if (previousStatus != status) { previousStatus = status; Report(status); }
                    await Task.Delay(OutputRetryDelay, token).ConfigureAwait(false);
                    continue;
                }
                if (previousStatus != status)
                {
                    previousStatus = status;
                    Report(status);
                }
                await Task.Delay(_profile.PollingIntervalMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            try
            {
                if (_output.IsConnected) _output.PushState(default, _profile.DeadZone);
            }
            catch (Exception ex) { LastError ??= ex.Message; }
            try { _output.Disconnect(); }
            catch (Exception ex) { LastError ??= ex.Message; }
            try { ReleaseInput(); }
            catch (Exception ex) { LastError ??= ex.Message; }
        }
    }

    private void Report(string message)
    {
        // A presentation callback must not prevent driver cleanup.
        try { _statusCallback?.Invoke(message); }
        catch { /* Presentation owner may already be shutting down. */ }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_loopTask is not null) await _loopTask.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await StopAsync().ConfigureAwait(false); }
        finally
        {
            _cts?.Dispose();
            try { ReleaseInput(); }
            finally { _output.Dispose(); }
        }
    }

    private void ReleaseInput()
    {
        if (Interlocked.Exchange(ref _inputReleased, 1) == 0) _input.Dispose();
    }
}
