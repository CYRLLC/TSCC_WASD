using System.Globalization;
using System.Text;

namespace TSCC_WASD.Core;

/// <summary>
/// Local diagnostic log: one file per day in %APPDATA%\TSCC_WASD\logs, the newest seven kept.
/// Never sent anywhere; users attach it to bug reports themselves. Logging never throws.
/// </summary>
public static class AppLog
{
    private const int KeepFiles = 7;
    private static readonly object Gate = new();

    /// <summary>Folder holding the log files. Tests point it at a temporary folder.</summary>
    public static string Directory { get; set; } = AppPaths.File("logs");

    public static string CurrentFile => Path.Combine(Directory, $"TSCC_WASD-{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    private static void Write(string level, string message)
    {
        var line = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append(' ').Append(level).Append(' ').Append(message).AppendLine().ToString();
        try
        {
            lock (Gate)
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.AppendAllText(CurrentFile, line, Encoding.UTF8);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    /// <summary>Deletes all but the newest <see cref="KeepFiles"/> log files.</summary>
    public static void Prune()
    {
        try
        {
            lock (Gate)
            {
                if (!System.IO.Directory.Exists(Directory)) return;
                foreach (var old in new DirectoryInfo(Directory).GetFiles("TSCC_WASD-*.log")
                             .OrderByDescending(f => f.Name, StringComparer.Ordinal).Skip(KeepFiles))
                    old.Delete();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
