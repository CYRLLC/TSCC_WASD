using Xunit;
using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services.Input;

namespace YnyrWASD.Tests;

public class Switch2ReportTests
{
    private static Switch2StickCalibration Calibration => Switch2StickCalibration.Nominal;
    private static byte[] Report()
    {
        var data = new byte[64];
        data[0] = 0x05;
        Pack(data, 11, 2048, 2048);
        Pack(data, 14, 2048, 2048);
        return data;
    }

    private static void Pack(byte[] data, int offset, int x, int y)
    {
        data[offset] = (byte)x;
        data[offset + 1] = (byte)((x >> 8) | ((y & 15) << 4));
        data[offset + 2] = (byte)(y >> 4);
    }

    [Theory]
    [InlineData(5, 0x04, GamepadButtonFlags.A)]
    [InlineData(5, 0x08, GamepadButtonFlags.B)]
    [InlineData(5, 0x01, GamepadButtonFlags.X)]
    [InlineData(5, 0x02, GamepadButtonFlags.Y)]
    [InlineData(5, 0x40, GamepadButtonFlags.RightShoulder)]
    [InlineData(6, 0x01, GamepadButtonFlags.Back)]
    [InlineData(6, 0x02, GamepadButtonFlags.Start)]
    [InlineData(6, 0x04, GamepadButtonFlags.RightThumb)]
    [InlineData(6, 0x08, GamepadButtonFlags.LeftThumb)]
    [InlineData(7, 0x01, GamepadButtonFlags.DPadDown)]
    [InlineData(7, 0x02, GamepadButtonFlags.DPadUp)]
    [InlineData(7, 0x04, GamepadButtonFlags.DPadRight)]
    [InlineData(7, 0x08, GamepadButtonFlags.DPadLeft)]
    [InlineData(7, 0x40, GamepadButtonFlags.LeftShoulder)]
    public void ButtonPositionsMatchDualShockLayout(int offset, byte bit, GamepadButtonFlags expected)
    {
        var data = Report();
        data[offset] = bit;
        Assert.True(Switch2ReportParser.TryParse(data, Calibration, Calibration, out var state));
        Assert.Equal(expected, state.Gamepad.Buttons);
        Assert.Equal(0, state.Gamepad.LeftThumbX);
        Assert.Equal(0, state.Gamepad.LeftThumbY);
    }

    [Fact]
    public void BothDigitalTriggersMapToFullRangeAndRelease()
    {
        var data = Report();
        data[5] = data[7] = 0x80;
        Assert.True(Switch2ReportParser.TryParse(data, Calibration, Calibration, out var down));
        Assert.Equal(255, down.Gamepad.LeftTrigger);
        Assert.Equal(255, down.Gamepad.RightTrigger);
        Assert.True(Switch2ReportParser.TryParse(Report(), Calibration, Calibration, out var up));
        Assert.Equal(0, up.Gamepad.LeftTrigger);
        Assert.Equal(0, up.Gamepad.RightTrigger);
    }

    [Fact]
    public void PackedAxesRetainXInputOrientationAndExtremes()
    {
        var data = Report();
        Pack(data, 11, 0, 4095);
        Pack(data, 14, 4095, 0);
        Assert.True(Switch2ReportParser.TryParse(data, Calibration, Calibration, out var state));
        Assert.Equal(short.MinValue, state.Gamepad.LeftThumbX);
        Assert.Equal(short.MaxValue, state.Gamepad.LeftThumbY);
        Assert.Equal(short.MaxValue, state.Gamepad.RightThumbX);
        Assert.Equal(short.MinValue, state.Gamepad.RightThumbY);
    }

    [Fact]
    public void CalibrationUsesStoredCenterAndIndependentTravelRanges()
    {
        var packed = new byte[9];
        Pack(packed, 0, 2000, 2100);
        Pack(packed, 3, 1000, 1200);
        Pack(packed, 6, 1100, 900);
        var calibration = Switch2StickCalibration.Parse(packed);
        Assert.True(calibration.IsValid);
        Assert.Equal(0, calibration.X.Normalize(2000));
        Assert.Equal(short.MaxValue, calibration.X.Normalize(3000));
        Assert.Equal(short.MinValue, calibration.X.Normalize(900));
        Assert.Equal(short.MaxValue, calibration.Y.Normalize(3300));
        Assert.Equal(short.MinValue, calibration.Y.Normalize(1200));
        Assert.Equal(short.MaxValue, calibration.X.Normalize(4095));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(63)]
    [InlineData(65)]
    [InlineData(128)]
    public void WrongReportLengthsAreRejected(int length)
    {
        var data = new byte[length];
        if (length > 0) data[0] = 5;
        Assert.False(Switch2ReportParser.TryParse(data, Calibration, Calibration, out _));
    }

    [Fact]
    public void UnknownReportOrInvalidCalibrationCannotGenerateInput()
    {
        var data = Report();
        data[0] = 0x30;
        Assert.False(Switch2ReportParser.TryParse(data, Calibration, Calibration, out _));
        Assert.False(Switch2ReportParser.TryParse(Report(), default, Calibration, out _));
        Assert.Throws<System.IO.InvalidDataException>(() => Switch2StickCalibration.Parse(new byte[8]));
    }

    [Fact]
    public void Switch2ProfileIsValidatedAndSnapshottedWithoutChangingLegacyValues()
    {
        Assert.Equal(0, (int)InputDeviceType.XInput);
        Assert.Equal(1, (int)InputDeviceType.DirectInput);
        Assert.Equal(2, (int)InputDeviceType.KeyboardMouse);
        var profile = new MappingProfile { InputType = InputDeviceType.Switch2ProUsb };
        Assert.Equal(InputDeviceType.Switch2ProUsb, profile.Snapshot().InputType);
        profile.InputType = InputDeviceType.DirectInput;
        Assert.Throws<ArgumentException>(profile.Validate);
    }
}
