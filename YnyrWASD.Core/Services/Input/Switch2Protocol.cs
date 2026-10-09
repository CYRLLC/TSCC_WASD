namespace YnyrWASD.Core.Services.Input;

/// <summary>Read-only calibration and volatile USB setup; no pairing/firmware/flash writes.</summary>
/// <remarks>Adapted protocol sequence from SDL_hidapi_switch2.c. See licenses/SDL.txt.</remarks>
internal static class Switch2Protocol
{
    internal static (Switch2StickCalibration Left, Switch2StickCalibration Right) Initialize(
        Switch2UsbControl usb, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var left = Switch2StickCalibration.Parse(usb.ReadFlash(0x13080).AsSpan(0x28));
        token.ThrowIfCancellationRequested();
        var right = Switch2StickCalibration.Parse(usb.ReadFlash(0x130C0).AsSpan(0x28));
        var userLeft = usb.ReadFlash(0x1FC040);
        if (userLeft[0] == 0xB2 && userLeft[1] == 0xA1)
        {
            var calibrated = Switch2StickCalibration.Parse(userLeft.AsSpan(2));
            if (calibrated.IsValid) left = calibrated;
        }
        token.ThrowIfCancellationRequested();
        var userRight = usb.ReadFlash(0x1FC080);
        if (userRight[0] == 0xB2 && userRight[1] == 0xA1)
        {
            var calibrated = Switch2StickCalibration.Parse(userRight.AsSpan(2));
            if (calibrated.IsValid) right = calibrated;
        }
        if (!left.IsValid || !right.IsValid) throw new IOException("NS2 Pro 搖桿校準資料無效。");

        byte[][] sequence =
        [
            [0x07, 0x91, 0, 1, 0, 0, 0, 0],
            [0x0C, 0x91, 0, 2, 0, 4, 0, 0, 0x27, 0, 0, 0],
            [0x11, 0x91, 0, 1, 0, 0, 0, 0],
            [0x0A, 0x91, 0, 8, 0, 0x14, 0, 0, 1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0x35, 0, 0x46, 0, 0, 0, 0, 0, 0, 0, 0],
            [0x0C, 0x91, 0, 4, 0, 4, 0, 0, 0x27, 0, 0, 0],
            [1, 0x91, 0, 0x0C, 0, 0, 0, 0],
            [1, 0x91, 0, 1, 0, 0, 0, 0],
            [8, 0x91, 0, 2, 0, 4, 0, 0, 1, 0, 0, 0],
            [3, 0x91, 0, 0x0A, 0, 4, 0, 0, 5, 0, 0, 0],
            [3, 0x91, 0, 0x0D, 0, 8, 0, 0, 1, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]
        ];
        foreach (var command in sequence)
        {
            token.ThrowIfCancellationRequested();
            usb.Exchange(command);
        }
        return (left, right);
    }
}
