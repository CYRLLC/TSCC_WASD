using System.Windows;
using TSCC_WASD.App.ViewModels;
using TSCC_WASD.Core;
using TSCC_WASD.Core.Services;

namespace TSCC_WASD.App;

public partial class App : Application
{
    private const string ShowEventName = @"Local\TSCC_WASD.Show";
    private Mutex? _instanceMutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showWait;
    private TrayIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, @"Local\TSCC_WASD.Desktop", out bool created);
        if (!created)
        {
            // Bring the running instance forward instead of starting a second mapper.
            if (EventWaitHandle.TryOpenExisting(ShowEventName, out var existing))
                using (existing) existing.Set();
            Shutdown();
            return;
        }
        base.OnStartup(e);

        // Before the rename this app was YnyrWASD; two mappers would fight over the same controllers.
        if (Mutex.TryOpenExisting(@"Local\" + AppPaths.LegacyName + ".Desktop", out var legacyInstance))
        {
            legacyInstance.Dispose();
            MessageBox.Show(L.T("舊版 YnyrWASD 仍在執行，請先關閉它再開啟 TSCC_WASD。",
                "The old YnyrWASD is still running. Close it before opening TSCC_WASD."), AppPaths.Name);
            Shutdown();
            return;
        }
        bool migrated = AppPaths.MigrateLegacyData();
        var settings = new AppSettingsStore().Load();
        try { StartupRegistration.MigrateLegacy(settings.StartWithWindows, Environment.ProcessPath!); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { }
        L.Apply(settings.Language); // Before any window or view model text is created.
        bool atSignIn = e.Args.Contains(StartupRegistration.StartupArgument, StringComparer.OrdinalIgnoreCase);

        var viewModel = new MainViewModel();
        if (migrated)
            viewModel.ShowNotice(L.T("已從舊版 YnyrWASD 搬移設定檔、校準與程式設定。", "Profiles, calibration and settings were moved over from YnyrWASD."));
        var window = new MainWindow(viewModel);
        MainWindow = window;
        _tray = new TrayIcon(viewModel, () => window.ShowFromTray(), () => window.Close());

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _showWait = ThreadPool.RegisterWaitForSingleObject(_showEvent,
            (_, _) => Dispatcher.BeginInvoke(() => window.ShowFromTray()), null, Timeout.Infinite, executeOnlyOnce: false);

        if (atSignIn && viewModel.MinimizeToTray)
            _tray.ShowBalloon(L.T("TSCC_WASD 已在背景執行。", "TSCC_WASD is running in the background."));
        else
            window.Show();
        _ = viewModel.AutoStartAsync(atSignIn);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showWait?.Unregister(null);
        _showEvent?.Dispose();
        _tray?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
