
namespace YnyrWASD.Core.Services.Input;

public interface IInputReader : IDisposable
{
    string Status => "XInput";
    bool TryGetState(out State state);
}
