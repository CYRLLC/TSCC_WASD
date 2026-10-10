using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

namespace TSCC_WASD.Core.Services.Setup;

/// <summary>An official driver installer, pinned to one release and its SHA-256.</summary>
public sealed record DriverPackage(string Id, string Name, string Version, string FileName, string Url, string Sha256, bool Required);

/// <summary>
/// Finds and runs the official ViGEmBus and HidHide installers. Release ZIPs carry them in a
/// <c>drivers</c> folder; otherwise they are downloaded from the pinned GitHub release. Either
/// way the file must match the SHA-256 in <c>drivers.json</c> before it is started.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DriverInstaller
{
    private const int ErrorCancelled = 1223; // The user declined the UAC prompt.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    /// <summary>Pinned installers, from the drivers.json manifest that package.ps1 also reads.</summary>
    public static IReadOnlyList<DriverPackage> Packages { get; } = LoadManifest();

    public static DriverPackage ViGEmBus => Packages.Single(p => p.Id == "vigembus");
    public static DriverPackage HidHide => Packages.Single(p => p.Id == "hidhide");

    /// <summary>Folder next to TSCC_WASD.exe where release ZIPs keep the bundled installers.</summary>
    public static string BundledDirectory => Path.Combine(AppContext.BaseDirectory, "drivers");

    private static string DownloadDirectory => Path.Combine(Path.GetTempPath(), AppPaths.Name, "drivers");

    /// <summary>Packages that are not installed yet, required ones first.</summary>
    public static IReadOnlyList<DriverPackage> Missing()
    {
        var missing = new List<DriverPackage>();
        if (!DependencyChecker.IsViGEmInstalled()) missing.Add(ViGEmBus);
        if (!DependencyChecker.IsHidHideInstalled()) missing.Add(HidHide);
        return missing;
    }

    /// <summary>True when the release ZIP carries this installer, so nothing has to be downloaded.</summary>
    public static bool IsBundled(DriverPackage package) =>
        File.Exists(Path.Combine(BundledDirectory, package.FileName));

    public static bool HasExpectedHash(string path, string sha256)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).Equals(sha256, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns a verified installer: the bundled copy, an earlier download, or a fresh download.</summary>
    public static async Task<string> GetInstallerAsync(DriverPackage package, CancellationToken token = default)
    {
        foreach (var dir in new[] { BundledDirectory, DownloadDirectory })
        {
            var candidate = Path.Combine(dir, package.FileName);
            if (File.Exists(candidate) && HasExpectedHash(candidate, package.Sha256)) return candidate;
        }

        Directory.CreateDirectory(DownloadDirectory);
        var target = Path.Combine(DownloadDirectory, package.FileName);
        var partial = target + ".part";
        using (var response = await Http.GetAsync(package.Url, HttpCompletionOption.ResponseHeadersRead, token))
        {
            response.EnsureSuccessStatusCode();
            await using var file = File.Create(partial);
            await response.Content.CopyToAsync(file, token);
        }
        if (!HasExpectedHash(partial, package.Sha256))
        {
            File.Delete(partial);
            throw new InvalidDataException(L.T($"下載的 {package.Name} 安裝檔與預期的 SHA-256 不符，已刪除。",
                $"The downloaded {package.Name} installer does not match the expected SHA-256 and was deleted."));
        }
        File.Move(partial, target, overwrite: true);
        return target;
    }

    /// <summary>
    /// Runs the installer elevated and waits for it. Returns its exit code, or null when the user
    /// declined the administrator prompt.
    /// </summary>
    public static async Task<int?> RunAsync(string installerPath, CancellationToken token = default)
    {
        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true, Verb = "runas" });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return null;
        }
        if (process is null) return null;
        using (process)
        {
            await process.WaitForExitAsync(token);
            return process.ExitCode;
        }
    }

    private static IReadOnlyList<DriverPackage> LoadManifest()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TSCC_WASD.Core.drivers.json")
            ?? throw new InvalidOperationException("drivers.json resource is missing.");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<DriverPackage>>(stream, options)
            ?? throw new InvalidOperationException("drivers.json is empty.");
    }
}
