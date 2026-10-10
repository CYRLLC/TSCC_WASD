using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace TSCC_WASD.Core.Services.Setup;

/// <summary>
/// Plain-text summary for bug reports: versions, drivers and the live input state. It holds no
/// user name or personal path, and is only copied to the clipboard when the user asks.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DiagnosticReport
{
    public static string Build(string inputSummary, bool mapping)
    {
        static string Driver(bool installed, string? version) =>
            installed ? $"installed {version ?? "(version unknown)"}" : "not installed";

        return new StringBuilder()
            .AppendLine($"TSCC_WASD {UpdateChecker.CurrentVersion.ToString(3)}")
            .AppendLine($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})")
            .AppendLine($".NET: {RuntimeInformation.FrameworkDescription}")
            .AppendLine($"UI language: {(L.Chinese ? "zh-TW" : "en")}")
            .AppendLine($"ViGEmBus: {Driver(DependencyChecker.IsViGEmInstalled(), DependencyChecker.ViGEmVersion())}")
            .AppendLine($"HidHide: {Driver(DependencyChecker.IsHidHideInstalled(), DependencyChecker.HidHideVersion())}")
            .AppendLine($"Steam running: {(SteamHelper.IsRunning ? "yes" : "no")}")
            .AppendLine($"Mapping: {(mapping ? "running" : "stopped")}")
            .AppendLine($"Input: {inputSummary}")
            .ToString();
    }
}
