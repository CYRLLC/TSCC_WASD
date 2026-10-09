using System.ComponentModel;
using System.Windows;
using TSCC_WASD.App.ViewModels;
using TSCC_WASD.Core;

namespace TSCC_WASD.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _closing;
    private bool _readyToClose;

    public MainWindow() : this(new MainViewModel()) { }

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Closing += OnClosing;
        StateChanged += OnStateChanged;
    }

    /// <summary>Restores the window from the notification area (or brings it forward).</summary>
    public void ShowFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Mapping keeps running; the tray icon brings the window back.
        if (WindowState == WindowState.Minimized && _viewModel.MinimizeToTray) Hide();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_readyToClose) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        IsEnabled = false;
        try
        {
            await _viewModel.DisposeAsync();
            // Controllers are unhidden now; Steam keeps running either way.
            await _viewModel.OfferSteamRestartAfterHidingAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.T("清理控制器時發生錯誤", "Error while cleaning up controllers"));
        }
        finally
        {
            _readyToClose = true;
            // Cleanup may finish synchronously; closing again inside Closing throws.
            _ = Dispatcher.BeginInvoke(Close);
        }
    }
}
