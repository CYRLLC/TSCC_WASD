using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace TSCC_WASD.Core.Services.Setup;

[SupportedOSPlatform("windows")]
public static class DependencyChecker
{
    private const string ViGEmRegistryPath = @"SYSTEM\CurrentControlSet\Services\ViGEmBus";
    private const string HidHideRegistryPath = @"SYSTEM\CurrentControlSet\Services\HidHide";

    public static bool IsViGEmInstalled() => IsInstalled(ViGEmRegistryPath);

    public static bool IsHidHideInstalled() => IsInstalled(HidHideRegistryPath);

    private static bool IsInstalled(string path)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            return key is not null;
        }
        catch (System.Security.SecurityException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static string? ViGEmVersion() => DriverFileVersion(ViGEmRegistryPath);

    public static string? HidHideVersion() => DriverFileVersion(HidHideRegistryPath);

    /// <summary>File version of the driver a service points at, e.g. 1.22.0.0; null when unknown.</summary>
    private static string? DriverFileVersion(string servicePath)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(servicePath);
            if (key?.GetValue("ImagePath") is not string image) return null;
            // Driver services use \SystemRoot\... or System32\... paths.
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var path = image.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(windows, image[@"\SystemRoot\".Length..])
                : image.StartsWith(@"\??\", StringComparison.Ordinal) ? image[4..]
                : Path.IsPathRooted(image) ? image : Path.Combine(windows, image);
            return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    public static string BuildStatusText()
    {
        var vigem = IsViGEmInstalled() ? L.T("已安裝", "installed") : L.T("未安裝", "not installed");
        var hidhide = IsHidHideInstalled() ? L.T("已安裝", "installed") : L.T("未安裝", "not installed");
        return $"ViGEmBus: {vigem} · HidHide: {hidhide}";
    }
}
