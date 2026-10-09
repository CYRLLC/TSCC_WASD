using System.Windows;
using YnyrWASD.App.ViewModels;
using YnyrWASD.Core;
using YnyrWASD.Core.Services;

namespace YnyrWASD.App;

public partial class App : Application
{
    private const string ShowEventName = @"Local\YnyrWASD.Show";
    private Mutex? _instanceMutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showWait;
    private TrayIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, @"Local\YnyrWASD.Desktop", out bool created);
        if (!created)
        {
            // Bring the running instance forward instead of starting a second mapper.
            if (EventWaitHandle.TryOpenExisting(ShowEventName, out var existing))
                using (existing) existing.Set();
            Shutdown();
            return;
        }
        base.OnStartup(e);

        var settings = new AppSettingsStore().Load();
        L.Apply(settings.Language); // Before any window or view model text is created.
        bool atSignIn = e.Args.Contains(StartupRegistration.StartupArgument, StringComparer.OrdinalIgnoreCase);

        var viewModel = new MainViewModel();
        var window = new MainWindow(viewModel);
        MainWindow = window;
        _tray = new TrayIcon(viewModel, () => window.ShowFromTray(), () => window.Close());

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _showWait = ThreadPool.RegisterWaitForSingleObject(_showEvent,
            (_, _) => Dispatcher.BeginInvoke(() => window.ShowFromTray()), null, Timeout.Infinite, executeOnlyOnce: false);

        if (atSignIn && viewModel.MinimizeToTray)
            _tray.ShowBalloon(L.T("YnyrWASD 已在背景執行。", "YnyrWASD is running in the background."));
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
