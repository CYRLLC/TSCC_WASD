using System.Runtime.Versioning;
using Microsoft.Win32;

namespace YnyrWASD.Core.Services.Setup;

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

    public static string BuildStatusText()
    {
        var vigem = IsViGEmInstalled() ? L.T("已安裝", "installed") : L.T("未安裝", "not installed");
        var hidhide = IsHidHideInstalled() ? L.T("已安裝", "installed") : L.T("未安裝", "not installed");
        return $"ViGEmBus: {vigem} · HidHide: {hidhide}";
    }
}
