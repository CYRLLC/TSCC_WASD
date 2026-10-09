using System.Runtime.Versioning;
using TSCC_WASD.Core.Models;
using TSCC_WASD.Core.Services.Input;
using TSCC_WASD.Core.Services.Setup;
using TSCC_WASD.Core.Services.VirtualControllers;

namespace TSCC_WASD.Core.Services.Mapping;

[SupportedOSPlatform("windows")]
public sealed class MappingCoordinator : IAsyncDisposable
{
    private static readonly TimeSpan HideRescanInterval = TimeSpan.FromSeconds(5);
    private readonly ProfileStore _profileStore;
    private readonly Func<HidHideGuard?> _hidHideFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private MappingSession? _session;
    private HidHideGuard? _hider;
    private CancellationTokenSource? _rescanCts;
    private Task? _rescanTask;
    private bool _disposed;

    public MappingCoordinator(ProfileStore? profileStore = null, Func<HidHideGuard?>? hidHideFactory = null)
    {
        _profileStore = profileStore ?? new ProfileStore();
        _hidHideFactory = hidHideFactory ?? HidHideGuard.TryCreate;
    }

    /// <summary>Undo HidHide changes left behind by a crash or forced exit.</summary>
    public string? RecoverHiddenControllers()
    {
        try
        {
            var guard = _hidHideFactory();
            if (guard is null || !guard.HasPendingRestore) return null;
            guard.Restore();
            return L.T("已還原上次未正常結束時隱藏的實體手把。", "Restored controllers left hidden by an unexpected exit.");
        }
        catch (Exception ex) { return L.T($"無法還原 HidHide 設定：{ex.Message}", $"Could not restore HidHide settings: {ex.Message}"); }
    }
    public IReadOnlyList<MappingProfile> LoadProfiles() => _profileStore.LoadProfiles();
    public void SaveProfiles(IEnumerable<MappingProfile> profiles) => _profileStore.SaveProfiles(profiles);
    public bool IsRunning => _session?.IsRunning == true;
    public string? LastError => _session?.LastError;
    public string InputSummary => _session?.InputSummary ?? L.T("尚未啟動映射", "Mapping not started");

    public async Task<(bool started, string message)> StartAsync(MappingProfile profile, Action<string>? statusCallback = null)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsRunning) return (false, L.T("已有映射在運作，請先停止。", "Mapping is already running; stop it first."));
            await ClearSessionAsync().ConfigureAwait(false);
            var snapshot = profile.Snapshot();
            var output = VirtualControllerFactory.Create(snapshot.OutputType);
            IInputReader input;
            try
            {
                input = snapshot.InputType switch
                {
                    InputDeviceType.Switch2ProUsb => new Switch2InputReader(),
                    InputDeviceType.XInput => new XInputReader(),
                    _ => new AutoInputReader()
                };
            }
            catch { output.Dispose(); throw; }
            _session = new MappingSession(snapshot, input, output, statusCallback);
            if (!_session.TryStart())
            {
                string message = _session.LastError ?? L.T("無法啟動映射。", "Could not start mapping.");
                await _session.DisposeAsync().ConfigureAwait(false);
                _session = null;
                return (false, message);
            }
            // Hide only after the virtual DS4 exists, so a failed start never leaves controllers hidden.
            string hideMessage = snapshot.HidePhysicalControllers ? StartHiding(snapshot.InputType) : "";
            return (true, L.T($"映射已啟動。{hideMessage}", $"Mapping started. {hideMessage}"));
        }
        catch (Exception ex)
        {
            if (_session is not null)
            {
                try { await _session.DisposeAsync().ConfigureAwait(false); }
                catch { /* Preserve the original startup error. */ }
                _session = null;
            }
            await StopHidingAsync().ConfigureAwait(false);
            return (false, L.T($"無法啟動：{ex.Message}", $"Could not start: {ex.Message}"));
        }
        finally { _gate.Release(); }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { await ClearSessionAsync().ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    /// <summary>When physical controllers were hidden for the current session, or null.</summary>
    public DateTime? HidingSince { get; private set; }

    /// <summary>
    /// Steam opened the controllers before they were hidden, so Steam Input still forwards them
    /// to games and prompts flip between Xbox and PS until Steam restarts.
    /// </summary>
    public bool SteamSeesPhysicalControllers => HidingSince is { } since && SteamHelper.StartedBefore(since);

    private string StartHiding(InputDeviceType inputType)
    {
        try
        {
            _hider = _hidHideFactory();
            if (_hider is null) return L.T("未安裝 HidHide：遊戲可能同時看到實體手把，按鍵圖示可能在 Xbox／PS 間跳動。", "HidHide is not installed: games may also see the physical controller and prompts may flip between Xbox and PS.");
            string appPath = Environment.ProcessPath ?? throw new InvalidOperationException(L.T("無法取得程式路徑。", "Could not determine the program path."));
            string message = _hider.Hide(appPath, inputType);
            HidingSince = DateTime.Now;
            var hider = _hider;
            _rescanCts = new CancellationTokenSource();
            var token = _rescanCts.Token;
            // Controllers turned on or plugged into a new port after start are hidden too.
            _rescanTask = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(HideRescanInterval);
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    try { hider.Hide(appPath, inputType); }
                    catch { /* Keep mapping; the next tick retries. */ }
                }
            }, token);
            return message;
        }
        catch (Exception ex) { return L.T($"無法自動隱藏實體手把：{ex.Message}", $"Could not hide physical controllers: {ex.Message}"); }
    }

    private async Task StopHidingAsync()
    {
        _rescanCts?.Cancel();
        if (_rescanTask is not null)
        {
            try { await _rescanTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _rescanCts?.Dispose();
        _rescanCts = null;
        _rescanTask = null;
        var hider = _hider;
        _hider = null;
        HidingSince = null;
        hider?.Restore();
    }

    private async Task ClearSessionAsync()
    {
        var session = _session;
        _session = null;
        try
        {
            if (session is not null) await session.DisposeAsync().ConfigureAwait(false);
        }
        finally { await StopHidingAsync().ConfigureAwait(false); }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            await ClearSessionAsync().ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
