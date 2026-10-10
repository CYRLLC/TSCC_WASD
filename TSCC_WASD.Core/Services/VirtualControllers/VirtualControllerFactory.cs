using TSCC_WASD.Core.Models;

namespace TSCC_WASD.Core.Services.VirtualControllers;

public static class VirtualControllerFactory
{
    public static IVirtualController Create(OutputControllerType type) =>
        type switch
        {
            OutputControllerType.DualShock4 => new DualShock4VirtualPad(),
            OutputControllerType.Xbox360 => new Xbox360VirtualPad(),
            _ => throw new NotSupportedException($"Unsupported output controller: {type}. Supported: DualShock 4, Xbox 360.")
        };
}
