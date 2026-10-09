using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using YnyrWASD.Core.Models;
using YnyrWASD.Core.Services;
using YnyrWASD.Core.Services.Mapping;
using YnyrWASD.Core.Services.Setup;

namespace YnyrWASD.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly MappingCoordinator _coordinator;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly List<AsyncRelayCommand> _commands = new();
    private MappingProfile? _selectedProfile;
    private string _statusMessage = "選擇設定檔後啟動映射。";
    private string _inputSummary = "尚未啟動映射";
    private bool _isRunning, _busy, _closing, _loadFailed, _steamRestartedWhileHidden;
    private string _dependencyStatus = DependencyChecker.BuildStatusText();

    public MainViewModel(ProfileStore? profileStore = null)
    {
        _coordinator = new MappingCoordinator(profileStore);
        StartCommand = Command(StartAsync, () => !IsRunning && SelectedProfile is not null);
        StopCommand = Command(StopAsync, () => IsRunning);
        RestartSteamCommand = Command(RestartSteamAsync, () => IsRunning);
        ReloadCommand = Command(() => { Reload(); return Task.CompletedTask; }, () => !IsRunning);
        SaveCommand = Command(() => { Save(); return Task.CompletedTask; }, () => !IsRunning && !_loadFailed);
        NewCommand = Command(() =>
        {
            var profile = new MappingProfile { Name = "新設定檔" };
            Profiles.Add(profile);
            SelectedProfile = profile;
            return Task.CompletedTask;
        }, () => !IsRunning && !_loadFailed);
        DeleteCommand = Command(() =>
        {
            if (SelectedProfile is not null) Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles.FirstOrDefault();
            StatusMessage = "已移除，按儲存套用至磁碟。";
            return Task.CompletedTask;
        }, () => !IsRunning && !_loadFailed && Profiles.Count > 1 && SelectedProfile is not null);
        ImportCommand = Command(() =>
        {
            var dialog = new OpenFileDialog { Filter = "JSON 設定檔|*.json" };
            if (dialog.ShowDialog() == true)
            {
                var imported = new ProfileStore(dialog.FileName).LoadProfiles();
                foreach (var profile in imported)
                {
                    profile.Id = Guid.NewGuid().ToString();
                    Profiles.Add(profile);
                }
                SelectedProfile = Profiles.Last();
                StatusMessage = "匯入完成，按儲存套用至磁碟。";
            }
            return Task.CompletedTask;
        }, () => !IsRunning && !_loadFailed);
        ExportCommand = Command(() =>
        {
            var dialog = new SaveFileDialog { Filter = "JSON 設定檔|*.json", FileName = "YnyrWASD-profiles.json" };
            if (dialog.ShowDialog() == true)
            {
                new ProfileStore(dialog.FileName).SaveProfiles(Profiles);
                StatusMessage = "匯出完成。";
            }
            return Task.CompletedTask;
        }, () => !IsRunning && Profiles.Count > 0);
        InstallDepsCommand = Command(() =>
        {
            Process.Start(new ProcessStartInfo("https://docs.nefarius.at/Downloads/") { UseShellExecute = true });
            StatusMessage = "已開啟官方下載頁。ViGEmBus 為必要依賴；HidHide 可選，需手動設定。";
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
        var command = new AsyncRelayCommand(async () =>
        {
            _busy = true;
            NotifyState();
            try { await action(); }
            catch (Exception ex) { StatusMessage = $"操作失敗：{ex.Message}"; }
            finally { _busy = false; NotifyState(); }
        }, () => !_closing && !_busy && (canExecute?.Invoke() ?? true));
        _commands.Add(command);
        return command;
    }

    public ObservableCollection<MappingProfile> Profiles { get; } = new();
    public IReadOnlyList<InputOption> InputOptions { get; } =
    [
        new(InputDeviceType.Auto, "自動偵測（NS2 Pro／Xbox，建議）"),
        new(InputDeviceType.XInput, "只用 Xbox／XInput"),
        new(InputDeviceType.Switch2ProUsb, "只用 Nintendo Switch 2 Pro（USB）")
    ];
    public sealed record InputOption(InputDeviceType Type, string Name);
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
            SelectedProfile = Profiles.FirstOrDefault();
            _loadFailed = false;
            StatusMessage = "設定檔已載入。";
        }
        catch (Exception ex)
        {
            _loadFailed = true;
            Profiles.Clear();
            SelectedProfile = null;
            StatusMessage = $"無法讀取設定檔，原始檔案已保留。請開啟設定資料夾修正 profiles.json 後重新載入：{ex.Message}";
        }
        DependencyStatus = DependencyChecker.BuildStatusText();
        NotifyState();
    }

    private void Save()
    {
        _coordinator.SaveProfiles(Profiles);
        StatusMessage = "設定已儲存，前一版保留為 profiles.json.bak。";
        System.Windows.Data.CollectionViewSource.GetDefaultView(Profiles).Refresh();
    }

    private async Task StartAsync()
    {
        if (SelectedProfile is null) return;
        var result = await _coordinator.StartAsync(SelectedProfile, UpdateStatusFromWorker);
        IsRunning = _coordinator.IsRunning;
        StatusMessage = result.message;
        if (IsRunning && _coordinator.SteamSeesPhysicalControllers)
        {
            var answer = MessageBox.Show(
                "Steam 在隱藏實體手把之前就已啟動，仍握有實體手把，會透過 Steam Input 轉給遊戲，" +
                "造成按鍵圖示在 Xbox／PS 之間交替。\n\n" +
                "要現在重新啟動 Steam 嗎？重啟後 Steam 只會看到虛擬 DS4，Steam 遊戲會顯示 PS 圖示。\n" +
                "（請先關閉正在執行的 Steam 遊戲；遊戲的 Steam Input 請保持啟用／預設。）",
                "YnyrWASD", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes) await RestartSteamAsync();
            else StatusMessage = $"{result.message} Steam 仍看得到實體手把，可稍後按「重新啟動 Steam」。";
        }
    }

    private async Task RestartSteamAsync()
    {
        StatusMessage = "正在重新啟動 Steam...";
        await SteamHelper.RestartAsync();
        _steamRestartedWhileHidden = true;
        StatusMessage = "Steam 已重新啟動，現在只看得到虛擬 DS4。停止映射或關閉程式時會詢問是否再重啟 Steam。";
    }

    private async Task StopAsync()
    {
        try { await _coordinator.StopAsync(); }
        finally { IsRunning = false; }
        StatusMessage = "映射已停止。";
        await OfferSteamRestartAfterHidingAsync();
    }

    /// <summary>
    /// A Steam started while controllers were hidden may not notice them once unhidden.
    /// Call after mapping has stopped; asks at most once per hidden period.
    /// </summary>
    public async Task OfferSteamRestartAfterHidingAsync()
    {
        if (!_steamRestartedWhileHidden) return;
        _steamRestartedWhileHidden = false;
        if (!SteamHelper.IsRunning) return;
        var answer = MessageBox.Show(
            "Steam 是在實體手把被隱藏期間重新啟動的，現在可能看不到實體手把。\n\n" +
            "要再重新啟動 Steam 一次，讓它重新認得實體手把嗎？\n（請先關閉正在執行的 Steam 遊戲。）",
            "YnyrWASD", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
        {
            StatusMessage = "映射已停止。之後若 Steam 看不到實體手把，重啟 Steam 或重新連接手把即可。";
            return;
        }
        StatusMessage = "正在重新啟動 Steam...";
        await SteamHelper.RestartAsync();
        StatusMessage = "映射已停止，Steam 已重新啟動並可看到實體手把。";
    }

    private void OnTick(object? sender, EventArgs e)
    {
        InputSummary = _coordinator.InputSummary;
        bool running = _coordinator.IsRunning;
        if (IsRunning && !running) StatusMessage = $"映射已結束：{_coordinator.LastError ?? "已停止"}";
        IsRunning = running;
    }

    private void UpdateStatusFromWorker(string message)
    {
        var dispatcher = Application.Current.Dispatcher;
        if (!dispatcher.HasShutdownStarted)
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
