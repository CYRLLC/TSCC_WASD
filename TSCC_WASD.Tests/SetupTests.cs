using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using TSCC_WASD.Core;
using TSCC_WASD.Core.Services;
using TSCC_WASD.Core.Services.Setup;

namespace TSCC_WASD.Tests;

public class SetupTests
{
    [Fact]
    public void DriverManifestPinsOfficialHttpsInstallers()
    {
        Assert.True(DriverInstaller.ViGEmBus.Required);
        Assert.False(DriverInstaller.HidHide.Required);
        foreach (var package in DriverInstaller.Packages)
        {
            Assert.StartsWith("https://github.com/nefarius/", package.Url);
            Assert.EndsWith("/" + package.FileName, package.Url);
            Assert.Matches("^[0-9a-f]{64}$", package.Sha256);
            Assert.Contains(package.Version, package.FileName);
        }
    }

    [Fact]
    public void InstallerHashMustMatchExactly()
    {
        string path = Path.Combine(Path.GetTempPath(), "TSCC_WASD-tests", Guid.NewGuid().ToString("N") + ".exe");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "installer");
        try
        {
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("installer")));
            Assert.True(DriverInstaller.HasExpectedHash(path, hash.ToLowerInvariant()));
            Assert.True(DriverInstaller.HasExpectedHash(path, hash));
            Assert.False(DriverInstaller.HasExpectedHash(path, new string('0', 64)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void HidHideOfferIsOnByDefault()
    {
        Assert.True(new AppSettings().OfferHidHideInstall);
    }

    [Fact]
    public void DiagnosticReportNamesVersionAndDrivers()
    {
        string report = DiagnosticReport.Build("NS2 Pro: idle", mapping: false);
        Assert.Contains($"TSCC_WASD {UpdateChecker.CurrentVersion.ToString(3)}", report);
        Assert.Contains("ViGEmBus:", report);
        Assert.Contains("HidHide:", report);
        Assert.Contains("Input: NS2 Pro: idle", report);
        Assert.DoesNotContain(Environment.UserName + "\\", report);
    }

    [Fact]
    public void LogAppendsAndKeepsTheNewestSevenFiles()
    {
        string original = AppLog.Directory;
        string dir = Path.Combine(Path.GetTempPath(), "TSCC_WASD-tests", Guid.NewGuid().ToString("N"));
        AppLog.Directory = dir;
        try
        {
            AppLog.Info("first");
            AppLog.Error("second", new InvalidOperationException("boom"));
            string text = File.ReadAllText(AppLog.CurrentFile);
            Assert.Contains("INFO first", text);
            Assert.Contains("ERROR second", text);
            Assert.Contains("boom", text);

            for (int day = 1; day <= 9; day++) File.WriteAllText(Path.Combine(dir, $"TSCC_WASD-2020010{day}.log"), "");
            AppLog.Prune();
            var kept = Directory.GetFiles(dir, "TSCC_WASD-*.log").Select(Path.GetFileName).ToList();
            Assert.Equal(7, kept.Count);
            Assert.Contains(Path.GetFileName(AppLog.CurrentFile), kept);
            Assert.DoesNotContain("TSCC_WASD-20200101.log", kept);
        }
        finally
        {
            AppLog.Directory = original;
            Directory.Delete(dir, recursive: true);
        }
    }

}
