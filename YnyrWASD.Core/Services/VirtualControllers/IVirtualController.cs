using YnyrWASD.Core.Services.Input;

namespace YnyrWASD.Core.Services.VirtualControllers;

public interface IVirtualController : IDisposable
{
    bool IsConnected { get; }
    string? LastError { get; }

    bool TryConnect(out string? error);
    void PushState(State state, double deadZone);
    void Disconnect();
}
