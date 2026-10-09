using System.Runtime.InteropServices;
using Xunit;
using TSCC_WASD.Core.Services.Input;

namespace TSCC_WASD.Tests;

public class XInputTests
{
    [Fact]
    public void NativeLayoutMatchesWindowsSdk()
    {
        Assert.Equal(12, Marshal.SizeOf<Gamepad>());
        Assert.Equal(16, Marshal.SizeOf<State>());
        Assert.Equal(4, Marshal.OffsetOf<State>(nameof(State.Gamepad)).ToInt32());
        Assert.Equal(10, Marshal.OffsetOf<Gamepad>(nameof(Gamepad.RightThumbY)).ToInt32());
    }

    [Fact]
    public void InboxApiLoadsWithOrWithoutAConnectedController()
    {
        using var reader = new XInputReader();
        reader.TryGetState(out _);
    }
}
