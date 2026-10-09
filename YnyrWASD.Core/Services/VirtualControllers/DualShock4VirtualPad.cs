using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using YnyrWASD.Core.Services.Input;
using YnyrWASD.Core.Services.Mapping;

namespace YnyrWASD.Core.Services.VirtualControllers;

public sealed class DualShock4VirtualPad : IVirtualController
{
    private readonly ViGEmClient _client;
    private readonly IDualShock4Controller _controller;
    private bool _connected;

    public DualShock4VirtualPad()
    {
        _client = new ViGEmClient();
        try
        {
            _controller = _client.CreateDualShock4Controller();
            _controller.AutoSubmitReport = false;
        }
        catch
        {
            _client.Dispose();
            throw;
        }
    }

    public bool IsConnected => _connected;
    public string? LastError { get; private set; }

    public bool TryConnect(out string? error)
    {
        try
        {
            _controller.Connect();
            _connected = true;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            LastError = ex.ToString();
            _connected = false;
            return false;
        }
    }

    public void Disconnect()
    {
        if (_connected)
        {
            _controller.Disconnect();
            _connected = false;
        }
    }

    public void PushState(State state, double deadZone)
    {
        if (!_connected)
        {
            return;
        }

        var gp = state.Gamepad;

        _controller.ResetReport();

        // Buttons
        _controller.SetButtonState(DualShock4Button.Cross, gp.Buttons.HasFlag(GamepadButtonFlags.A));
        _controller.SetButtonState(DualShock4Button.Circle, gp.Buttons.HasFlag(GamepadButtonFlags.B));
        _controller.SetButtonState(DualShock4Button.Square, gp.Buttons.HasFlag(GamepadButtonFlags.X));
        _controller.SetButtonState(DualShock4Button.Triangle, gp.Buttons.HasFlag(GamepadButtonFlags.Y));

        _controller.SetButtonState(DualShock4Button.ShoulderLeft, gp.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
        _controller.SetButtonState(DualShock4Button.ShoulderRight, gp.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
        _controller.SetButtonState(DualShock4Button.TriggerLeft, gp.LeftTrigger > 10);
        _controller.SetButtonState(DualShock4Button.TriggerRight, gp.RightTrigger > 10);

        _controller.SetButtonState(DualShock4Button.ThumbLeft, gp.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
        _controller.SetButtonState(DualShock4Button.ThumbRight, gp.Buttons.HasFlag(GamepadButtonFlags.RightThumb));

        _controller.SetButtonState(DualShock4Button.Share, gp.Buttons.HasFlag(GamepadButtonFlags.Back));
        _controller.SetButtonState(DualShock4Button.Options, gp.Buttons.HasFlag(GamepadButtonFlags.Start));

        // DPad
        _controller.SetDPadDirection(MapDPad(gp.Buttons));

        // Axes
        _controller.SetAxisValue(DualShock4Axis.LeftThumbX, NormalizeAxis(gp.LeftThumbX, deadZone));
        _controller.SetAxisValue(DualShock4Axis.LeftThumbY, NormalizeAxis(gp.LeftThumbY, deadZone, invert: true));
        _controller.SetAxisValue(DualShock4Axis.RightThumbX, NormalizeAxis(gp.RightThumbX, deadZone));
        _controller.SetAxisValue(DualShock4Axis.RightThumbY, NormalizeAxis(gp.RightThumbY, deadZone, invert: true));

        // Triggers as analog sliders
        _controller.SetSliderValue(DualShock4Slider.LeftTrigger, gp.LeftTrigger);
        _controller.SetSliderValue(DualShock4Slider.RightTrigger, gp.RightTrigger);

        _controller.SubmitReport();
    }

    private static DualShock4DPadDirection MapDPad(GamepadButtonFlags buttons)
    {
        var up = buttons.HasFlag(GamepadButtonFlags.DPadUp);
        var down = buttons.HasFlag(GamepadButtonFlags.DPadDown);
        var left = buttons.HasFlag(GamepadButtonFlags.DPadLeft);
        var right = buttons.HasFlag(GamepadButtonFlags.DPadRight);

        return (up, down, left, right) switch
        {
            (true, false, false, false) => DualShock4DPadDirection.North,
            (true, false, true, false) => DualShock4DPadDirection.Northwest,
            (false, false, true, false) => DualShock4DPadDirection.West,
            (false, true, true, false) => DualShock4DPadDirection.Southwest,
            (false, true, false, false) => DualShock4DPadDirection.South,
            (false, true, false, true) => DualShock4DPadDirection.Southeast,
            (false, false, false, true) => DualShock4DPadDirection.East,
            (true, false, false, true) => DualShock4DPadDirection.Northeast,
            _ => DualShock4DPadDirection.None
        };
    }

    private static byte NormalizeAxis(short value, double deadZone, bool invert = false)
    {
        return AxisConverter.Convert(value, deadZone, invert);
    }

    public void Dispose()
    {
        try { Disconnect(); }
        finally
        {
            try { _controller.Dispose(); }
            finally { _client.Dispose(); }
        }
    }
}
