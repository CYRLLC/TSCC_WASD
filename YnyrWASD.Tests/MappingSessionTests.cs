using Xunit;
using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services.Input;
using YnyrWASD.Core.Services.Mapping;
using YnyrWASD.Core.Services.VirtualControllers;

namespace YnyrWASD.Tests;

public class MappingSessionTests
{
    [Fact]
    public async Task DisconnectReleasesHeldButtonsAndStopDisposesResources()
    {
        var input = new FakeInput();
        var output = new FakeOutput();
        var session = new MappingSession(new MappingProfile(), input, output);
        Assert.True(session.TryStart());
        await output.NeutralAfterPress.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await session.DisposeAsync();
        await session.DisposeAsync();
        Assert.False(session.IsRunning);
        Assert.False(output.IsConnected);
        Assert.Equal(1, output.DisposeCount);
        Assert.True(input.Disposed);
        Assert.Equal(GamepadButtonFlags.None, output.LastState.Gamepad.Buttons);
    }

    [Fact]
    public async Task DriverFailureReconnectsAndKeepsMapping()
    {
        var output = new FakeOutput { FailPushes = 1 };
        var input = new FakeInput();
        await using var session = new MappingSession(new MappingProfile(), input, output);
        Assert.True(session.TryStart());
        await output.Reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(session.IsRunning);
        Assert.Equal("driver failure", session.LastError);
        await session.StopAsync();
        Assert.False(session.IsRunning);
        Assert.False(output.IsConnected);
        Assert.True(input.Disposed);
    }

    [Fact]
    public async Task ThrowingInputIsTreatedAsDisconnectedWithoutEndingSession()
    {
        var output = new FakeOutput();
        await using var session = new MappingSession(new MappingProfile(), new ThrowingInput(), output);
        Assert.True(session.TryStart());
        await output.Pushed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(session.IsRunning);
        Assert.Equal("read failure", session.LastError);
        Assert.Equal(GamepadButtonFlags.None, output.LastState.Gamepad.Buttons);
    }

    [Fact]
    public async Task FailedConnectionCanBeDisposed()
    {
        var output = new FakeOutput { FailConnect = true };
        var session = new MappingSession(new MappingProfile(), new FakeInput(), output);
        Assert.False(session.TryStart());
        await session.DisposeAsync();
        Assert.Equal(1, output.DisposeCount);
    }

    [Fact]
    public async Task ThrowingStatusCallbackDoesNotInterruptMapping()
    {
        var output = new FakeOutput();
        await using var session = new MappingSession(new MappingProfile(), new FakeInput(), output,
            _ => throw new InvalidOperationException("UI closed"));
        Assert.True(session.TryStart());
        await output.NeutralAfterPress.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await session.StopAsync();
        Assert.Null(session.LastError);
    }

    [Theory]
    [InlineData(OutputControllerType.DualSense)]
    [InlineData(OutputControllerType.Xbox360)]
    [InlineData((OutputControllerType)99)]
    public void UnsupportedOutputFailsExplicitly(OutputControllerType type)
        => Assert.Throws<NotSupportedException>(() => VirtualControllerFactory.Create(type));

    private sealed class FakeInput : IInputReader
    {
        private int _reads;
        public bool Disposed { get; private set; }
        public bool TryGetState(out State state)
        {
            state = new State { Gamepad = new Gamepad { Buttons = GamepadButtonFlags.A } };
            return Interlocked.Increment(ref _reads) == 1;
        }
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public async Task ForwardsRumbleAndMotionAndStopsRumbleOnDispose()
    {
        var input = new RumbleInput();
        var output = new FakeOutput();
        var session = new MappingSession(new MappingProfile(), input, output);
        Assert.True(session.TryStart());
        await output.Pushed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new MotionSample(1, 2, 3, 4, 5, 6), output.LastMotion);
        output.RaiseRumble(200, 50);
        Assert.Equal((200, 50), input.Last);
        await session.DisposeAsync();
        Assert.Equal((0, 0), input.Last);
        Assert.Null(output.LastMotion); // The final neutral report carries no motion.
    }

    [Fact]
    public async Task RumbleForwardingCanBeDisabled()
    {
        var input = new RumbleInput();
        var output = new FakeOutput();
        await using var session = new MappingSession(new MappingProfile { ForwardRumble = false }, input, output);
        Assert.True(session.TryStart());
        output.RaiseRumble(200, 50);
        Assert.Equal((0, 0), input.Last);
    }

    private sealed class RumbleInput : IInputReader, IRumbleTarget, IMotionSource
    {
        public (int Large, int Small) Last { get; private set; }
        public bool TryGetState(out State state) { state = default; return true; }
        public bool TryGetMotion(out MotionSample motion) { motion = new MotionSample(1, 2, 3, 4, 5, 6); return true; }
        public void SetRumble(byte large, byte small) => Last = (large, small);
        public void Dispose() { }
    }

    private sealed class ThrowingInput : IInputReader
    {
        public bool TryGetState(out State state) => throw new InvalidOperationException("read failure");
        public void Dispose() { }
    }

    private sealed class FakeOutput : IVirtualController
    {
        public bool IsConnected { get; private set; }
        public string? LastError => null;
        public int FailPushes { get; set; }
        private int _connects;
        public bool FailConnect { get; init; }
        public int DisposeCount { get; private set; }
        public State LastState { get; private set; }
        private bool _pressed;
        public TaskCompletionSource NeutralAfterPress { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Reconnected { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Pushed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool TryConnect(out string? error)
        {
            error = FailConnect ? "unavailable" : null;
            if (!FailConnect && ++_connects > 1) Reconnected.TrySetResult();
            return IsConnected = !FailConnect;
        }
        public event Action<byte, byte>? RumbleRequested;
        public void RaiseRumble(byte large, byte small) => RumbleRequested?.Invoke(large, small);
        public MotionSample? LastMotion { get; private set; }
        public void PushState(State state, double deadZone, MotionSample? motion = null)
        {
            LastMotion = motion;
            if (FailPushes > 0) { FailPushes--; throw new InvalidOperationException("driver failure"); }
            LastState = state;
            Pushed.TrySetResult();
            if (state.Gamepad.Buttons == GamepadButtonFlags.A) _pressed = true;
            if (_pressed && state.Gamepad.Buttons == GamepadButtonFlags.None) NeutralAfterPress.TrySetResult();
        }
        public void Disconnect() => IsConnected = false;
        public void Dispose() { DisposeCount++; Disconnect(); }
    }
}
