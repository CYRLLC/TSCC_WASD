using System.Text.Json;

namespace YnyrWASD.Core.Services.Input;

/// <summary>
/// Remembers the NS2 Pro factory/user stick calibration read over USB. When Steam owns the USB
/// control interface the calibration cannot be read, and the nominal ±2048 range would leave
/// a fully pushed stick far short of full output.
/// </summary>
public sealed class Switch2CalibrationCache
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path;

    public Switch2CalibrationCache(string? path = null) => _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YnyrWASD", "ns2-calibration.json");

    public void Save(Switch2StickCalibration left, Switch2StickCalibration right)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(new Entry(left, right), Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Cache only. */ }
    }

    public bool TryLoad(out Switch2StickCalibration left, out Switch2StickCalibration right)
    {
        left = right = default;
        try
        {
            if (!File.Exists(_path)) return false;
            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(_path));
            if (entry is null || !entry.Left.IsValid || !entry.Right.IsValid) return false;
            (left, right) = (entry.Left, entry.Right);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed record Entry(Switch2StickCalibration Left, Switch2StickCalibration Right);
}
