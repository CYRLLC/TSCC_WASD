using System.Diagnostics;

namespace TSCC_WASD.Core.Services.Setup;

/// <summary>An anti-cheat system reported to refuse virtual controllers, and what to tell the player.</summary>
public sealed record AntiCheat(string ProcessName, string Name, string AdviceZh, string AdviceEn);

/// <summary>
/// Spots running anti-cheat systems that are known to reject ViGEm virtual controllers, so the player
/// learns why a game will not start instead of guessing. Only systems with public reports are listed;
/// most anti-cheat (for example Easy Anti-Cheat or BattlEye games) accepts virtual pads.
/// </summary>
public static class AntiCheatWatcher
{
    public static IReadOnlyList<AntiCheat> Known { get; } =
    [
        // Reported for the Battlefield 6 beta and Battlefield 2042, which refuse to run alongside
        // DS4Windows (a ViGEm virtual pad); EA staff said such tools risk a ban.
        new("EAAntiCheat.GameService", "EA Javelin",
            "EA 反作弊（Javelin）正在執行。《戰地風雲 6》《戰地風雲 2042》等 EA 遊戲據報會拒絕在使用虛擬手把（ViGEmBus）時啟動，" +
            "甚至有封鎖帳號的風險。玩這類遊戲前請按「停止」並關閉 TSCC_WASD，直接使用實體手把或 Steam Input。",
            "EA's anti-cheat (Javelin) is running. EA games such as Battlefield 6 and Battlefield 2042 reportedly refuse to start " +
            "while a virtual controller (ViGEmBus) is in use and may risk a ban. Before playing them, click Stop and close " +
            "TSCC_WASD, then use the physical controller or Steam Input."),
    ];

    /// <summary>The first known anti-cheat that is running, or null.</summary>
    public static AntiCheat? FindRunning(Func<string, bool>? isRunning = null)
    {
        isRunning ??= IsProcessRunning;
        return Known.FirstOrDefault(a => isRunning(a.ProcessName));
    }

    private static bool IsProcessRunning(string name)
    {
        var processes = Process.GetProcessesByName(name);
        foreach (var process in processes) process.Dispose();
        return processes.Length > 0;
    }
}
