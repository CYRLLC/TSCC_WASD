using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services.Input;
using YnyrWASD.Core.Services.VirtualControllers;

namespace YnyrWASD.Core.Services.Mapping;

public sealed class MappingCoordinator : IAsyncDisposable
{
    private readonly ProfileStore _profileStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private MappingSession? _session;
    private bool _disposed;

    public MappingCoordinator(ProfileStore? profileStore = null) => _profileStore = profileStore ?? new ProfileStore();
    public IReadOnlyList<MappingProfile> LoadProfiles() => _profileStore.LoadProfiles();
    public void SaveProfiles(IEnumerable<MappingProfile> profiles) => _profileStore.SaveProfiles(profiles);
    public bool IsRunning => _session?.IsRunning == true;
    public string? LastError => _session?.LastError;
    public string InputSummary => _session?.InputSummary ?? "尚未啟動映射";

    public async Task<(bool started, string message)> StartAsync(MappingProfile profile, Action<string>? statusCallback = null)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsRunning) return (false, "已有映射在運作，請先停止。");
            if (_session is not null)
            {
                await _session.DisposeAsync().ConfigureAwait(false);
                _session = null;
            }
            var snapshot = profile.Snapshot();
            var output = VirtualControllerFactory.Create(snapshot.OutputType);
            IInputReader input;
            try
            {
                input = snapshot.InputType == InputDeviceType.Switch2ProUsb
                    ? new Switch2InputReader() : new XInputReader();
            }
            catch { output.Dispose(); throw; }
            _session = new MappingSession(snapshot, input, output, statusCallback);
            if (_session.TryStart()) return (true, "映射已啟動。");
            string message = _session.LastError ?? "無法啟動映射。";
            await _session.DisposeAsync().ConfigureAwait(false);
            _session = null;
            return (false, message);
        }
        catch (Exception ex)
        {
            if (_session is not null)
            {
                try { await _session.DisposeAsync().ConfigureAwait(false); }
                catch { /* Preserve the original startup error. */ }
                _session = null;
            }
            return (false, $"無法啟動：{ex.Message}");
        }
        finally { _gate.Release(); }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { await ClearSessionAsync().ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    private async Task ClearSessionAsync()
    {
        var session = _session;
        _session = null;
        if (session is not null) await session.DisposeAsync().ConfigureAwait(false);
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
