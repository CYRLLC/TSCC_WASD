using Xunit;
using YnyrWASD.Core.Services.Input;

namespace YnyrWASD.Tests;

public class AutoInputReaderTests
{
    private static State Pressed(GamepadButtonFlags buttons) => new() { Gamepad = new Gamepad { Buttons = buttons } };

    [Fact]
    public void IdleControllerDoesNotStealFromTheOneInUse()
    {
        var ns2 = new FakeSource { Available = true };
        var xbox = new FakeSource { Available = true };
        using var reader = new AutoInputReader(ns2, xbox);

        Assert.True(reader.TryGetState(out _));
        Assert.Equal("NS2 Pro", reader.ActiveSource);

        xbox.State = Pressed(GamepadButtonFlags.A);
        Assert.True(reader.TryGetState(out var state));
        Assert.Equal("Xbox／XInput", reader.ActiveSource);
        Assert.Equal(GamepadButtonFlags.A, state.Gamepad.Buttons);

        // Releasing the button keeps Xbox active although NS2 still streams neutral reports.
        xbox.State = default;
        reader.TryGetState(out _);
        Assert.Equal("Xbox／XInput", reader.ActiveSource);

        ns2.State = Pressed(GamepadButtonFlags.B);
        reader.TryGetState(out state);
        Assert.Equal("NS2 Pro", reader.ActiveSource);
        Assert.Equal(GamepadButtonFlags.B, state.Gamepad.Buttons);
    }

    [Fact]
    public void ActiveSourceDoesNotSwitchWhileHeld()
    {
        var ns2 = new FakeSource { Available = true, State = Pressed(GamepadButtonFlags.A) };
        var xbox = new FakeSource { Available = true, State = Pressed(GamepadButtonFlags.B) };
        using var reader = new AutoInputReader(ns2, xbox);
        reader.TryGetState(out var state);
        Assert.Equal("NS2 Pro", reader.ActiveSource);
        Assert.Equal(GamepadButtonFlags.A, state.Gamepad.Buttons);
    }

    [Fact]
    public void FallsBackWhenActiveDisconnectsOrThrows()
    {
        var ns2 = new FakeSource { Available = true };
        var xbox = new FakeSource { Available = true };
        using var reader = new AutoInputReader(ns2, xbox);
        reader.TryGetState(out _);
        ns2.Throw = true;
        Assert.True(reader.TryGetState(out _));
        Assert.Equal("Xbox／XInput", reader.ActiveSource);

        xbox.Available = false;
        Assert.False(reader.TryGetState(out var state));
        Assert.Null(reader.ActiveSource);
        Assert.Equal(default, state);
    }

    [Fact]
    public void SmallStickDriftIsNotTreatedAsUse()
    {
        Assert.False(AutoInputReader.IsInUse(new State { Gamepad = new Gamepad { LeftThumbX = 3000, RightTrigger = 10 } }));
        Assert.True(AutoInputReader.IsInUse(new State { Gamepad = new Gamepad { LeftThumbY = short.MinValue } }));
        Assert.True(AutoInputReader.IsInUse(new State { Gamepad = new Gamepad { LeftTrigger = 200 } }));
    }

    [Fact]
    public void DisposesBothSources()
    {
        var ns2 = new FakeSource();
        var xbox = new FakeSource();
        new AutoInputReader(ns2, xbox).Dispose();
        Assert.True(ns2.Disposed && xbox.Disposed);
    }

    private sealed class FakeSource : IInputReader
    {
        public bool Available { get; set; }
        public bool Throw { get; set; }
        public State State { get; set; }
        public bool Disposed { get; private set; }
        public bool TryGetState(out State state)
        {
            if (Throw) throw new InvalidOperationException("gone");
            state = Available ? State : default;
            return Available;
        }
        public void Dispose() => Disposed = true;
    }
}
