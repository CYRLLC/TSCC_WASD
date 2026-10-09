using System.Text.Json;

namespace YnyrWASD.Core.Services.Input;

/// <summary>
/// Stores NS2 Pro stick calibrations. The factory one is remembered whenever it can be read over USB
/// (Steam owning the USB control interface prevents that). A user calibration from the calibration
/// wizard takes priority over both and is never overwritten by the factory one.
/// </summary>
public sealed class Switch2CalibrationCache
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path;
    private readonly object _gate = new();

    public Switch2CalibrationCache(string? path = null) => _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YnyrWASD", "ns2-calibration.json");

    public void SaveFactory(Switch2StickCalibration left, Switch2StickCalibration right) =>
        Update(file => file with { Factory = new Pair(left, right) });

    /// <summary>Raised after the user calibration is saved or cleared, so a running reader can apply it.</summary>
    public static event Action? UserCalibrationChanged;

    public void SaveUser(Switch2StickCalibration left, Switch2StickCalibration right)
    {
        Update(file => file with { User = new Pair(left, right) });
        UserCalibrationChanged?.Invoke();
    }

    public void ClearUser()
    {
        Update(file => file with { User = null });
        UserCalibrationChanged?.Invoke();
    }

    public bool TryLoadFactory(out Switch2StickCalibration left, out Switch2StickCalibration right) =>
        TryGet(Read()?.Factory, out left, out right);

    public bool TryLoadUser(out Switch2StickCalibration left, out Switch2StickCalibration right) =>
        TryGet(Read()?.User, out left, out right);

    private static bool TryGet(Pair? pair, out Switch2StickCalibration left, out Switch2StickCalibration right)
    {
        left = pair?.Left ?? default;
        right = pair?.Right ?? default;
        return pair is not null && left.IsValid && right.IsValid;
    }

    private void Update(Func<FileData, FileData> change)
    {
        lock (_gate)
        {
            try
            {
                var data = change(Read() ?? new FileData(null, null));
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                string temp = _path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
                File.Move(temp, _path, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Cache only. */ }
        }
    }

    private FileData? Read()
    {
        try
        {
            return File.Exists(_path) ? JsonSerializer.Deserialize<FileData>(File.ReadAllText(_path)) : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record Pair(Switch2StickCalibration Left, Switch2StickCalibration Right);
    private sealed record FileData(Pair? Factory, Pair? User);
}
