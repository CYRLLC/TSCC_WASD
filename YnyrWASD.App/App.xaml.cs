using System.Configuration;
using System.Data;
using System.Windows;

namespace YnyrWASD.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, @"Local\YnyrWASD.Desktop", out bool created);
        if (!created)
        {
            MessageBox.Show("YnyrWASD 已在執行中。", "YnyrWASD");
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
