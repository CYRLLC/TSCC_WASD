using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace YnyrWASD.Core.Services.Setup;

/// <summary>
/// Steam keeps the device handles it opened before HidHide cloaking started, so Steam Input
/// would keep forwarding the physical controller to games. Restarting Steam while cloaked
/// leaves it with only the virtual DS4, which it reports to games as a PlayStation controller.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SteamHelper
{
    /// <summary>True if a Steam client started before <paramref name="time"/> is still running.</summary>
    public static bool StartedBefore(DateTime time)
    {
        foreach (var process in Process.GetProcessesByName("steam"))
        {
            using (process)
            {
                try { if (process.StartTime < time) return true; }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
            }
        }
        return false;
    }

    public static async Task RestartAsync(CancellationToken token = default)
    {
        string exe = FindSteamExe() ?? throw new FileNotFoundException("找不到 steam.exe。");
        if (Process.GetProcessesByName("steam").Length > 0)
        {
            using (Process.Start(new ProcessStartInfo(exe) { ArgumentList = { "-shutdown" }, UseShellExecute = false })) { }
            var deadline = DateTime.UtcNow.AddSeconds(45);
            while (Process.GetProcessesByName("steam").Length > 0)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Steam 沒有在 45 秒內結束；若有遊戲正在執行，請先關閉。");
                await Task.Delay(500, token).ConfigureAwait(false);
            }
        }
        using (Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true })) { }
    }

    private static string? FindSteamExe()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamExe") is string path && File.Exists(path)) return Path.GetFullPath(path);
        string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe");
        return File.Exists(fallback) ? fallback : null;
    }
}
