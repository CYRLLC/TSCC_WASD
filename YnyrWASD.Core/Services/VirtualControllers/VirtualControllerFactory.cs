using YnyrWASD.Core.Models;

namespace YnyrWASD.Core.Services.VirtualControllers;

public static class VirtualControllerFactory
{
    public static IVirtualController Create(OutputControllerType type) =>
        type switch
        {
            OutputControllerType.DualShock4 => new DualShock4VirtualPad(),
            _ => throw new NotSupportedException($"Unsupported output controller: {type}. Only DualShock4 is supported.")
        };
}
