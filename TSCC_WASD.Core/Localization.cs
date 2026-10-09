using System.Globalization;

namespace TSCC_WASD.Core;

/// <summary>
/// Two-language UI text. Strings stay next to the code that uses them so both versions are
/// reviewed together. The language is fixed at startup from settings or the Windows UI culture.
/// </summary>
public static class L
{
    public static bool Chinese { get; private set; } =
        CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    /// <param name="language">"auto", "zh-TW" or "en".</param>
    public static void Apply(string? language) => Chinese = language switch
    {
        "zh-TW" => true,
        "en" => false,
        _ => CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
    };

    public static string T(string zh, string en) => Chinese ? zh : en;
}
