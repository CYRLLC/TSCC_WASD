using System.Windows.Input;
using YnyrWASD.App.ViewModels;
using YnyrWASD.Core;
using Forms = System.Windows.Forms;

namespace YnyrWASD.App;

/// <summary>Notification-area icon: double-click restores the window; the menu starts, stops or exits.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly MainViewModel _viewModel;
    private readonly Forms.ToolStripMenuItem _start, _stop;

    public TrayIcon(MainViewModel viewModel, Action show, Action exit)
    {
        _viewModel = viewModel;
        _start = new Forms.ToolStripMenuItem(L.T("啟動映射", "Start mapping"), null, (_, _) => Run(viewModel.StartCommand));
        _stop = new Forms.ToolStripMenuItem(L.T("停止映射", "Stop mapping"), null, (_, _) => Run(viewModel.StopCommand));
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(L.T("顯示視窗", "Show window"), null, (_, _) => show());
        menu.Items.Add(_start);
        menu.Items.Add(_stop);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(L.T("結束", "Exit"), null, (_, _) => exit());
        menu.Opening += (_, _) =>
        {
            _start.Enabled = viewModel.StartCommand.CanExecute(null);
            _stop.Enabled = viewModel.StopCommand.CanExecute(null);
        };
        _icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => show();
        viewModel.PropertyChanged += OnViewModelChanged;
        UpdateText();
    }

    private static void Run(ICommand command)
    {
        if (command.CanExecute(null)) command.Execute(null);
    }

    private void OnViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsRunning)) UpdateText();
    }

    private void UpdateText()
    {
        string state = _viewModel.IsRunning ? L.T("映射中", "mapping") : L.T("已停止", "stopped");
        _icon.Text = $"YnyrWASD · {state}"; // NotifyIcon text is limited to 63 characters.
    }

    public void ShowBalloon(string text) => _icon.ShowBalloonTip(3000, "YnyrWASD", text, Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _icon.Visible = false;
        _icon.Dispose();
    }
}
