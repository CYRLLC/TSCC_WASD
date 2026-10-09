using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace TSCC_WASD.Core.Services.Setup;

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

    /// <summary>
    /// True if the running Steam client started at or after <paramref name="time"/>. Such a Steam never saw
    /// the controllers that were hidden then, and may not notice them once they are unhidden.
    /// </summary>
    public static bool StartedSince(DateTime time)
    {
        foreach (var process in Process.GetProcessesByName("steam"))
        {
            using (process)
            {
                try { if (process.StartTime >= time) return true; }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
            }
        }
        return false;
    }

    public static bool IsRunning
    {
        get
        {
            var processes = Process.GetProcessesByName("steam");
            foreach (var process in processes) process.Dispose();
            return processes.Length > 0;
        }
    }

    /// <summary>Starts Steam if it is not running. Returns false when Steam is not installed.</summary>
    public static bool LaunchIfNotRunning(bool silent)
    {
        if (IsRunning) return true;
        string? exe = FindSteamExe();
        if (exe is null) return false;
        Start(exe, silent);
        return true;
    }

    private static void Start(string exe, bool silent)
    {
        var info = new ProcessStartInfo(exe) { UseShellExecute = true };
        if (silent) info.ArgumentList.Add("-silent");
        using (Process.Start(info)) { }
    }

    /// <param name="silent">Start Steam minimized to the tray, as Steam does at sign-in.</param>
    public static async Task RestartAsync(bool silent = false, CancellationToken token = default)
    {
        string exe = FindSteamExe() ?? throw new FileNotFoundException(L.T("找不到 steam.exe。", "steam.exe was not found."));
        if (Process.GetProcessesByName("steam").Length > 0)
        {
            using (Process.Start(new ProcessStartInfo(exe) { ArgumentList = { "-shutdown" }, UseShellExecute = false })) { }
            var deadline = DateTime.UtcNow.AddSeconds(45);
            while (Process.GetProcessesByName("steam").Length > 0)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException(L.T("Steam 沒有在 45 秒內結束；若有遊戲正在執行，請先關閉。", "Steam did not exit within 45 seconds; close any running game first."));
                await Task.Delay(500, token).ConfigureAwait(false);
            }
        }
        Start(exe, silent);
    }

    private static string? FindSteamExe()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamExe") is string path && File.Exists(path)) return Path.GetFullPath(path);
        string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe");
        return File.Exists(fallback) ? fallback : null;
    }
}
