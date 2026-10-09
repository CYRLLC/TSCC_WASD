namespace YnyrWASD.Core.Services.Input;

/// <summary>
/// Reads NS2 Pro (USB) and XInput together and follows whichever controller the player is using.
/// The active source only changes when it disconnects or the other one receives deliberate input,
/// so an idle controller that still streams neutral reports never steals control.
/// </summary>
public sealed class AutoInputReader : IInputReader
{
    private const int TriggerThreshold = 30;
    private const int StickThreshold = 12000;
    private readonly IInputReader[] _sources;
    private readonly string[] _names = ["NS2 Pro", "Xbox／XInput"];
    private volatile int _active = -1;
    private volatile string _lastError = "";
    private bool _disposed;

    public AutoInputReader() : this(new Switch2InputReader(), new XInputReader()) { }

    public AutoInputReader(IInputReader switch2, IInputReader xinput) => _sources = [switch2, xinput];

    public string Status => _active switch
    {
        0 => $"自動偵測 → {_sources[0].Status}",
        1 => "自動偵測 → Xbox／XInput",
        _ => $"自動偵測：尋找手把中（NS2 Pro：{_sources[0].Status}；XInput：未連線{_lastError}）"
    };

    /// <summary>Name of the source currently being mapped, or null while none is available.</summary>
    public string? ActiveSource => _active < 0 ? null : _names[_active];

    public bool TryGetState(out State state)
    {
        Span<bool> available = stackalloc bool[_sources.Length];
        var states = new State[_sources.Length];
        for (int i = 0; i < _sources.Length; i++) available[i] = SafeRead(_sources[i], out states[i]);

        int active = _active;
        if (active < 0 || !available[active])
        {
            // Current source vanished: prefer one in use, then the first available.
            active = -1;
            for (int i = 0; i < _sources.Length && active < 0; i++)
                if (available[i] && IsInUse(states[i])) active = i;
            for (int i = 0; i < _sources.Length && active < 0; i++)
                if (available[i]) active = i;
        }
        else if (!IsInUse(states[active]))
        {
            for (int i = 0; i < _sources.Length; i++)
                if (i != active && available[i] && IsInUse(states[i])) { active = i; break; }
        }
        _active = active;
        state = active < 0 ? default : states[active];
        return active >= 0;
    }

    private bool SafeRead(IInputReader source, out State state)
    {
        try { return source.TryGetState(out state); }
        catch (Exception ex)
        {
            // One failing backend must not stop the other from being mapped.
            _lastError = $"（{ex.Message}）";
            state = default;
            return false;
        }
    }

    public static bool IsInUse(State state)
    {
        var gp = state.Gamepad;
        return gp.Buttons != GamepadButtonFlags.None
            || gp.LeftTrigger > TriggerThreshold || gp.RightTrigger > TriggerThreshold
            || Math.Abs((int)gp.LeftThumbX) > StickThreshold || Math.Abs((int)gp.LeftThumbY) > StickThreshold
            || Math.Abs((int)gp.RightThumbX) > StickThreshold || Math.Abs((int)gp.RightThumbY) > StickThreshold;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _sources[0].Dispose(); }
        finally { _sources[1].Dispose(); }
    }
}
