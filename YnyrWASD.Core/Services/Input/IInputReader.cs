
namespace YnyrWASD.Core.Services.Input;

public interface IInputReader : IDisposable
{
    string Status => "XInput";
    bool TryGetState(out State state);
}

/// <summary>Input that also reports gyro/accelerometer data.</summary>
public interface IMotionSource
{
    bool TryGetMotion(out MotionSample motion);
}

/// <summary>Input that can play rumble requested by the game through the virtual controller.</summary>
public interface IRumbleTarget
{
    /// <param name="large">Low-frequency (left) motor, 0–255.</param>
    /// <param name="small">High-frequency (right) motor, 0–255.</param>
    void SetRumble(byte large, byte small);
}
