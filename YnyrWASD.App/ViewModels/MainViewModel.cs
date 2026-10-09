using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using YnyrWASD.Core;
using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services;
using YnyrWASD.Core.Services.Mapping;
using YnyrWASD.Core.Services.Setup;

namespace YnyrWASD.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly MappingCoordinator _coordinator;
    private readonly AppSettingsStore _settingsStore;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly List<AsyncRelayCommand> _commands = new();
    private MappingProfile? _selectedProfile;
    private string _statusMessage = L.T("選擇設定檔後啟動映射。", "Choose a profile and start mapping.");
    private string _inputSummary = L.T("尚未啟動映射", "Mapping not started");
    private bool _isRunning, _busy, _closing, _loadFailed;
    private DateTime? _hiddenSince;
    private string _dependencyStatus = DependencyChecker.BuildStatusText();

    public MainViewModel(ProfileStore? profileStore = null, AppSettingsStore? settingsStore = null)
    {
        _coordinator = new MappingCoordinator(profileStore);
        _settingsStore = settingsStore ?? new AppSettingsStore();
        Settings = _settingsStore.Load();
        StartCommand = Command(() => StartAsync(automatic: false, atSignIn: false), () => !IsRunning && SelectedProfile is not null);
        StopCommand = Command(StopAsync, () => IsRunning);
        RestartSteamCommand = Command(() => RestartSteamAsync(silent: false), () => IsRunning);
        ReloadCommand = Command(() => { Reload(); return Task.CompletedTask; }, () => !IsRunning);
        SaveCommand = Command(() => { Save(); return Task.CompletedTask; }, () => !IsRunning && !_loadFailed);
        NewCommand = Command(() =>
        {
            var profile = new MappingProfile { Name = L.T("新設定檔", "New profile") };
            Profiles.Add(profile);
            SelectedProfile = profile;
            return Task.CompletedTask;
        }, () => !IsRunning && !_loadFailed);
        DeleteCommand = Command(() =>
        {
            if (SelectedProfile is not null) Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles.FirstOrDefault();
            StatusMessage = L.T("已移除，按儲存套用至磁碟。", "Removed. Click Save all to apply.");
            return Task.CompletedTask;
        }, () => !IsRunning && !_loadFailed && Profiles.Count > 1 && SelectedProfile is not null);
        ImportCommand = Command(() =>
        {
            var dialog = new OpenFileDialog { Filter = L.T("JSON 設定檔|*.json", "JSON profiles|*.json") };
            if (dialog.ShowDialog() == true)
            {
                var imported = new ProfileStore(dialog.FileName).LoadProfiles();
                foreach (var profile in imported)
                {
                    profile.Id = Guid.NewGuid().ToString();
                    Profiles.Add(profile);
                }
                SelectedProfile = Profiles.Last();
                StatusMessage = L.T("匯入完成，按儲存套用至磁碟。", "Imported. Click Save all to apply.");
            }
            return Task.CompletedTask;
        }, () => !IsRunning && !_loadFailed);
        ExportCommand = Command(() =>
        {
            var dialog = new SaveFileDialog { Filter = L.T("JSON 設定檔|*.json", "JSON profiles|*.json"), FileName = "YnyrWASD-profiles.json" };
            if (dialog.ShowDialog() == true)
            {
                new ProfileStore(dialog.FileName).SaveProfiles(Profiles);
                StatusMessage = L.T("匯出完成。", "Exported.");
            }
            return Task.CompletedTask;
        }, () => !IsRunning && Profiles.Count > 0);
        InstallDepsCommand = Command(() =>
        {
            Process.Start(new ProcessStartInfo("https://docs.nefarius.at/Downloads/") { UseShellExecute = true });
            StatusMessage = L.T("已開啟官方下載頁。ViGEmBus 為必要依賴；HidHide 強烈建議安裝（不需手動設定）。",
                "Opened the official downloads. ViGEmBus is required; HidHide is strongly recommended (no setup needed).");
            return Task.CompletedTask;
        });
        OpenProfilesCommand = Command(() =>
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YnyrWASD");
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { path }, UseShellExecute = true });
            return Task.CompletedTask;
        });
        Reload();
        if (_coordinator.RecoverHiddenControllers() is { } recovered) StatusMessage = recovered;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private AsyncRelayCommand Command(Func<Task> action, Func<bool>? canExecute = null)
    {
        var command = new AsyncRelayCommand(() => RunGuardedAsync(action), () => !_closing && !_busy && (canExecute?.Invoke() ?? true));
        _commands.Add(command);
        return command;
    }

    private async Task RunGuardedAsync(Func<Task> action)
    {
        _busy = true;
        NotifyState();
        try { await action(); }
        catch (Exception ex) { StatusMessage = L.T($"操作失敗：{ex.Message}", $"Failed: {ex.Message}"); }
        finally { _busy = false; NotifyState(); }
    }

    public ObservableCollection<MappingProfile> Profiles { get; } = new();
    public IReadOnlyList<InputOption> InputOptions { get; } =
    [
        new(InputDeviceType.Auto, L.T("自動偵測（NS2 Pro／Xbox，建議）", "Auto-detect (NS2 Pro / Xbox, recommended)")),
        new(InputDeviceType.XInput, L.T("只用 Xbox／XInput", "Xbox / XInput only")),
        new(InputDeviceType.Switch2ProUsb, L.T("只用 Nintendo Switch 2 Pro（USB）", "Nintendo Switch 2 Pro (USB) only"))
    ];
    public sealed record InputOption(InputDeviceType Type, string Name);
    public IReadOnlyList<LanguageOption> LanguageOptions { get; } =
    [
        new("auto", L.T("跟隨 Windows", "Follow Windows")),
        new("zh-TW", "繁體中文"),
        new("en", "English")
    ];
    public sealed record LanguageOption(string Code, string Name);

    public MappingProfile? SelectedProfile
    {
        get => _selectedProfile;
        set { if (SetField(ref _selectedProfile, value)) NotifyState(); }
    }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public string InputSummary { get => _inputSummary; private set => SetField(ref _inputSummary, value); }
    public string DependencyStatus { get => _dependencyStatus; private set => SetField(ref _dependencyStatus, value); }
    public bool IsRunning
    {
        get => _isRunning;
        private set { if (SetField(ref _isRunning, value)) NotifyState(); }
    }
    public bool CanEdit => !IsRunning && !_busy && !_closing && !_loadFailed;

    public AppSettings Settings { get; }

    public bool StartWithWindows
    {
        get => Settings.StartWithWindows;
        set
        {
            if (Settings.StartWithWindows == value) return;
            try
            {
                StartupRegistration.Set(value, Environment.ProcessPath ?? throw new InvalidOperationException());
                Settings.StartWithWindows = value;
                SaveSettings();
            }
            catch (Exception ex) { StatusMessage = L.T($"無法變更開機啟動：{ex.Message}", $"Could not change sign-in launch: {ex.Message}"); }
            RaisePropertyChanged();
        }
    }
    public bool StartMappingOnLaunch { get => Settings.StartMappingOnLaunch; set => SetSetting(() => Settings.StartMappingOnLaunch = value); }
    public bool MinimizeToTray { get => Settings.MinimizeToTray; set => SetSetting(() => Settings.MinimizeToTray = value); }
    public bool ManageSteam { get => Settings.ManageSteam; set => SetSetting(() => Settings.ManageSteam = value); }
    public bool LaunchSteamAfterAutoStart { get => Settings.LaunchSteamAfterAutoStart; set => SetSetting(() => Settings.LaunchSteamAfterAutoStart = value); }
    public string Language
    {
        get => Settings.Language;
        set
        {
            SetSetting(() => Settings.Language = value);
            StatusMessage = L.T("語言會在下次開啟程式時套用。", "The language applies the next time YnyrWASD opens.");
        }
    }

    private void SetSetting(Action apply, [System.Runtime.CompilerServices.CallerMemberName] string? property = null)
    {
        apply();
        SaveSettings();
        RaisePropertyChanged(property);
    }

    private void SaveSettings()
    {
        try { _settingsStore.Save(Settings); }
        catch (Exception ex) { StatusMessage = L.T($"無法儲存程式設定：{ex.Message}", $"Could not save settings: {ex.Message}"); }
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RestartSteamCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand InstallDepsCommand { get; }
    public ICommand OpenProfilesCommand { get; }

    private void Reload()
    {
        try
        {
            var profiles = _coordinator.LoadProfiles();
            Profiles.Clear();
            foreach (var profile in profiles) Profiles.Add(profile);
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == Settings.LastProfileId) ?? Profiles.FirstOrDefault();
            _loadFailed = false;
            StatusMessage = L.T("設定檔已載入。", "Profiles loaded.");
        }
        catch (Exception ex)
        {
            _loadFailed = true;
            Profiles.Clear();
            SelectedProfile = null;
            StatusMessage = L.T($"無法讀取設定檔，原始檔案已保留。請開啟設定資料夾修正 profiles.json 後重新載入：{ex.Message}",
                $"Could not read profiles; the file was kept. Open the settings folder, fix profiles.json and reload: {ex.Message}");
        }
        DependencyStatus = DependencyChecker.BuildStatusText();
        NotifyState();
    }

    private void Save()
    {
        _coordinator.SaveProfiles(Profiles);
        StatusMessage = L.T("設定已儲存，前一版保留為 profiles.json.bak。", "Saved. The previous version is kept as profiles.json.bak.");
        System.Windows.Data.CollectionViewSource.GetDefaultView(Profiles).Refresh();
    }

    /// <summary>Starts mapping without user interaction when the settings ask for it.</summary>
    /// <param name="atSignIn">Launched by Windows at sign-in (<c>--startup</c>).</param>
    public Task AutoStartAsync(bool atSignIn)
    {
        if (!atSignIn && !Settings.StartMappingOnLaunch) return Task.CompletedTask;
        if (IsRunning || SelectedProfile is null) return Task.CompletedTask;
        return RunGuardedAsync(() => StartAsync(automatic: true, atSignIn));
    }

    private async Task StartAsync(bool automatic, bool atSignIn)
    {
        if (SelectedProfile is null) return;
        var result = await _coordinator.StartAsync(SelectedProfile, UpdateStatusFromWorker);
        IsRunning = _coordinator.IsRunning;
        StatusMessage = result.message;
        if (!IsRunning) return;
        Settings.LastProfileId = SelectedProfile.Id;
        SaveSettings();
        _hiddenSince = _coordinator.HidingSince;
        if (_hiddenSince is null) return;

        if (_coordinator.SteamSeesPhysicalControllers)
        {
            if (automatic && Settings.ManageSteam)
            {
                await RestartSteamAsync(silent: atSignIn);
                return;
            }
            if (automatic) return;
            var answer = MessageBox.Show(
                L.T("Steam 在隱藏實體手把之前就已啟動，仍握有實體手把，會透過 Steam Input 轉給遊戲，造成按鍵圖示在 Xbox／PS 之間交替。\n\n" +
                    "要現在重新啟動 Steam 嗎？重啟後 Steam 只會看到虛擬 DS4，Steam 遊戲會顯示 PS 圖示。\n" +
                    "（請先關閉正在執行的 Steam 遊戲；遊戲的 Steam Input 請保持啟用／預設。）",
                    "Steam started before the physical controllers were hidden, so it still holds them and Steam Input forwards " +
                    "them to games, making prompts alternate between Xbox and PS.\n\n" +
                    "Restart Steam now? Afterwards Steam only sees the virtual DS4 and Steam games show PS prompts.\n" +
                    "(Close any running Steam game first, and keep the game's Steam Input on default/enabled.)"),
                "YnyrWASD", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes) await RestartSteamAsync(silent: false);
            else StatusMessage = L.T($"{result.message} Steam 仍看得到實體手把，可稍後按「重新啟動 Steam」。",
                $"{result.message} Steam can still see the physical controller; click Restart Steam later.");
        }
        else if (automatic && Settings.LaunchSteamAfterAutoStart)
        {
            if (SteamHelper.LaunchIfNotRunning(silent: atSignIn))
                StatusMessage = L.T($"{result.message} 已在隱藏手把後啟動 Steam。", $"{result.message} Steam was started after hiding the controllers.");
        }
    }

    private async Task RestartSteamAsync(bool silent)
    {
        StatusMessage = L.T("正在重新啟動 Steam...", "Restarting Steam...");
        await SteamHelper.RestartAsync(silent);
        StatusMessage = L.T("Steam 已重新啟動，現在只看得到虛擬 DS4。", "Steam restarted and now only sees the virtual DS4.");
    }

    private async Task StopAsync()
    {
        try { await _coordinator.StopAsync(); }
        finally { IsRunning = false; }
        StatusMessage = L.T("映射已停止。", "Mapping stopped.");
        await OfferSteamRestartAfterHidingAsync();
    }

    /// <summary>
    /// A Steam that started while controllers were hidden may not notice them once unhidden.
    /// Call after mapping has stopped; asks at most once per hidden period. Steam keeps running either way.
    /// </summary>
    public async Task OfferSteamRestartAfterHidingAsync()
    {
        var since = _hiddenSince;
        _hiddenSince = null;
        if (since is null || !SteamHelper.StartedSince(since.Value)) return;
        var answer = MessageBox.Show(
            L.T("Steam 是在實體手把被隱藏期間啟動的，現在可能看不到實體手把。\n\n" +
                "要再重新啟動 Steam 一次，讓它重新認得實體手把嗎？\n（請先關閉正在執行的 Steam 遊戲。）",
                "Steam started while the physical controllers were hidden and may not see them now.\n\n" +
                "Restart Steam once more so it detects them again?\n(Close any running Steam game first.)"),
            "YnyrWASD", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
        {
            StatusMessage = L.T("映射已停止。之後若 Steam 看不到實體手把，重啟 Steam 或重新連接手把即可。",
                "Mapping stopped. If Steam doesn't see the controller later, restart Steam or reconnect the controller.");
            return;
        }
        StatusMessage = L.T("正在重新啟動 Steam...", "Restarting Steam...");
        await SteamHelper.RestartAsync();
        StatusMessage = L.T("映射已停止，Steam 已重新啟動並可看到實體手把。", "Mapping stopped; Steam restarted and can see the controller.");
    }

    private void OnTick(object? sender, EventArgs e)
    {
        InputSummary = _coordinator.InputSummary;
        bool running = _coordinator.IsRunning;
        if (IsRunning && !running)
            StatusMessage = L.T($"映射已結束：{_coordinator.LastError ?? "已停止"}", $"Mapping ended: {_coordinator.LastError ?? "stopped"}");
        IsRunning = running;
    }

    private void UpdateStatusFromWorker(string message)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.HasShutdownStarted)
            dispatcher.BeginInvoke(() => { if (!_closing) StatusMessage = message; });
    }

    private void NotifyState()
    {
        RaisePropertyChanged(nameof(CanEdit));
        foreach (var command in _commands) command.RaiseCanExecuteChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _closing = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        NotifyState();
        await _coordinator.DisposeAsync();
    }
}
