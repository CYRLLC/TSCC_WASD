using System.IO;
using Xunit;
using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services.Setup;

namespace YnyrWASD.Tests;

public class HidHideGuardTests
{
    private const string Ns2 = @"HID\VID_057E&PID_2069&MI_00\a&37d9c433&0&0000";
    private const string Xbox = @"HID\{00001812-0000-1000-8000-00805f9b34fb}&Dev&VID_045e&PID_0b13&REV_0509&686ce65b1cfd&IG_00\d&23b31df3&0&0000";
    private const string Sony = @"HID\VID_054C&PID_05C4&REV_0100\2&1b721325&0&0000";
    private const string App = @"C:\Apps\YnyrWASD.App.exe";

    private static string Gaming(params string[] paths) =>
        "[" + string.Join(",", paths.Select(p =>
            "{\"friendlyName\":\"x\",\"devices\":[{\"present\":true,\"deviceInstancePath\":\"" + p.Replace(@"\", @"\\") + "\"}]}")) + "]";

    [Fact]
    public void HidesMatchingControllersThenRestoresOnlyItsOwnChanges()
    {
        var cli = new FakeCli { Gaming = Gaming(Ns2, Xbox, Sony), Hidden = [Xbox] };
        var guard = new HidHideGuard(cli.Run, TempRecord());

        string message = guard.Hide(App, InputDeviceType.Auto);
        Assert.Contains("1", message);
        Assert.True(guard.HasPendingRestore);
        Assert.Equal(["--app-reg", App, "--dev-hide", Ns2, "--cloak-on"], cli.Changes.Single());

        // A rescan with nothing new does not issue changes.
        guard.Hide(App, InputDeviceType.Auto);
        Assert.Single(cli.Changes);

        guard.Restore();
        Assert.Equal(["--dev-unhide", Ns2, "--app-unreg", App, "--cloak-off"], cli.Changes.Last());
        Assert.False(guard.HasPendingRestore);
    }

    [Fact]
    public void KeepsUserSettingsThatWereAlreadyPresent()
    {
        var cli = new FakeCli { Gaming = Gaming(Ns2, Xbox), CloakOn = true, Apps = [App] };
        var guard = new HidHideGuard(cli.Run, TempRecord());
        guard.Hide(App, InputDeviceType.XInput);
        Assert.Equal(["--dev-hide", Xbox, "--cloak-on"], cli.Changes.Single());
        guard.Restore();
        Assert.Equal(["--dev-unhide", Xbox], cli.Changes.Last());
    }

    [Fact]
    public void RecordSurvivesRestartForCrashRecovery()
    {
        string record = TempRecord();
        var cli = new FakeCli { Gaming = Gaming(Ns2) };
        new HidHideGuard(cli.Run, record).Hide(App, InputDeviceType.Switch2ProUsb);
        var afterRestart = new HidHideGuard(cli.Run, record);
        Assert.True(afterRestart.HasPendingRestore);
        afterRestart.Restore();
        Assert.Equal(["--dev-unhide", Ns2, "--app-unreg", App, "--cloak-off"], cli.Changes.Last());
    }

    [Fact]
    public void InverseModeIsLeftAlone()
    {
        var cli = new FakeCli { Gaming = Gaming(Ns2), Inverse = true };
        var guard = new HidHideGuard(cli.Run, TempRecord());
        Assert.Contains("反向", guard.Hide(App, InputDeviceType.Auto));
        Assert.Empty(cli.Changes);
        Assert.False(guard.HasPendingRestore);
    }

    private static string TempRecord() =>
        Path.Combine(Path.GetTempPath(), "YnyrWASD-tests", Guid.NewGuid().ToString("N"), "hidhide-restore.json");

    private sealed class FakeCli
    {
        public string Gaming { get; init; } = "[]";
        public bool CloakOn { get; init; }
        public bool Inverse { get; init; }
        public string[] Hidden { get; init; } = [];
        public string[] Apps { get; init; } = [];
        public List<string[]> Changes { get; } = new();

        public string Run(IReadOnlyList<string> args) => args[0] switch
        {
            "--inv-state" => Inverse ? "--inv-on" : "--inv-off",
            "--cloak-state" => CloakOn ? "--cloak-on" : "--cloak-off",
            "--app-list" => string.Join("\n", Apps.Select(a => $"--app-reg \"{a}\"")),
            "--dev-list" => string.Join("\n", Hidden.Select(d => $"--dev-hide \"{d}\"")),
            "--dev-gaming" => Gaming,
            _ => Record(args)
        };

        private string Record(IReadOnlyList<string> args) { Changes.Add(args.ToArray()); return ""; }
    }
}
