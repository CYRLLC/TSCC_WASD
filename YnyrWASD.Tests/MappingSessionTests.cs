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
    public async Task DriverFailureEndsLoopAndDisconnects()
    {
        var output = new FakeOutput { FailPush = true };
        var input = new FakeInput();
        await using var session = new MappingSession(new MappingProfile(), input, output);
        Assert.True(session.TryStart());
        await output.Disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await session.StopAsync();
        Assert.False(session.IsRunning);
        Assert.Equal("driver failure", session.LastError);
        Assert.True(input.Disposed);
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

    private sealed class FakeOutput : IVirtualController
    {
        public bool IsConnected { get; private set; }
        public string? LastError => null;
        public bool FailPush { get; init; }
        public bool FailConnect { get; init; }
        public int DisposeCount { get; private set; }
        public State LastState { get; private set; }
        private bool _pressed;
        public TaskCompletionSource NeutralAfterPress { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Disconnected { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool TryConnect(out string? error)
        {
            error = FailConnect ? "unavailable" : null;
            return IsConnected = !FailConnect;
        }
        public void PushState(State state, double deadZone)
        {
            if (FailPush) throw new InvalidOperationException("driver failure");
            LastState = state;
            if (state.Gamepad.Buttons == GamepadButtonFlags.A) _pressed = true;
            if (_pressed && state.Gamepad.Buttons == GamepadButtonFlags.None) NeutralAfterPress.TrySetResult();
        }
        public void Disconnect() { IsConnected = false; Disconnected.TrySetResult(); }
        public void Dispose() { DisposeCount++; Disconnect(); }
    }
}
