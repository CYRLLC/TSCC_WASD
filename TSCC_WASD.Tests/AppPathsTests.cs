using System.IO;
using Xunit;
using TSCC_WASD.Core;

namespace TSCC_WASD.Tests;

public class AppPathsTests
{
    [Fact]
    public void MovesTheOldDataFolderOnceAndNeverOverwrites()
    {
        string root = Path.Combine(Path.GetTempPath(), "TSCC_WASD-tests", Guid.NewGuid().ToString("N"));
        string legacy = Path.Combine(root, AppPaths.LegacyName), current = Path.Combine(root, AppPaths.Name);
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "profiles.json"), "old");

        Assert.True(AppPaths.MigrateLegacyData(root));
        Assert.False(Directory.Exists(legacy));
        Assert.Equal("old", File.ReadAllText(Path.Combine(current, "profiles.json")));

        // A second old folder appearing later must not replace the new data.
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "profiles.json"), "stale");
        Assert.False(AppPaths.MigrateLegacyData(root));
        Assert.Equal("old", File.ReadAllText(Path.Combine(current, "profiles.json")));

        Assert.False(AppPaths.MigrateLegacyData(Path.Combine(root, "missing")));
    }
}
