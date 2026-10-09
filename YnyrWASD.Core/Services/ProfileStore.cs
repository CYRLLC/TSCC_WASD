using System.Text.Json;
using System.Text.Json.Serialization;
using YnyrWASD.Core.Models;

namespace YnyrWASD.Core.Services;

public sealed class ProfileStore
{
    private readonly string _profilePath;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProfileStore(string? profilePath = null)
    {
        _profilePath = Path.GetFullPath(profilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YnyrWASD", "profiles.json"));
    }

    public IReadOnlyList<MappingProfile> LoadProfiles()
    {
        if (!File.Exists(_profilePath))
        {
            var defaults = new[] { new MappingProfile() };
            SaveProfiles(defaults);
            return defaults;
        }
        // Never replace an unreadable or malformed user file with defaults.
        var profiles = JsonSerializer.Deserialize<List<MappingProfile>>(File.ReadAllText(_profilePath), Options)
            ?? throw new InvalidDataException(L.T("設定檔不能是 null。", "Profiles cannot be null."));
        Validate(profiles);
        return profiles;
    }

    public void SaveProfiles(IEnumerable<MappingProfile> profiles)
    {
        var snapshot = profiles.ToList();
        Validate(snapshot);
        string json = JsonSerializer.Serialize(snapshot, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(_profilePath)!);
        string temp = _profilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var writer = new StreamWriter(stream, leaveOpen: true);
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(_profilePath)) File.Replace(temp, _profilePath, _profilePath + ".bak");
            else File.Move(temp, _profilePath);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void Validate(List<MappingProfile> profiles)
    {
        if (profiles.Count == 0) throw new InvalidDataException(L.T("至少需要一個設定檔。", "At least one profile is required."));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            if (profile is null) throw new InvalidDataException(L.T("設定檔項目不能是 null。", "A profile entry cannot be null."));
            profile.Validate();
            if (!ids.Add(profile.Id)) throw new InvalidDataException(L.T("設定檔 ID 不能重複。", "Profile IDs must be unique."));
        }
    }
}
