using YnyrWASD.Core.Services.Input;

namespace YnyrWASD.Core.Services.VirtualControllers;

public interface IVirtualController : IDisposable
{
    bool IsConnected { get; }
    string? LastError { get; }

    /// <summary>Raised when a game sets rumble: (large/low-frequency, small/high-frequency), 0–255.</summary>
    event Action<byte, byte>? RumbleRequested;

    bool TryConnect(out string? error);
    void PushState(State state, double deadZone, MotionSample? motion = null);
    void Disconnect();
}
