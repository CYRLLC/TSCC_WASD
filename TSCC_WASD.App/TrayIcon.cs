using System.Runtime.InteropServices;
using System.Windows.Input;
using TSCC_WASD.App.ViewModels;
using TSCC_WASD.Core;
using Forms = System.Windows.Forms;

namespace TSCC_WASD.App;

/// <summary>
/// Notification-area icon: double-click restores the window; the menu starts, stops or exits.
/// A green dot on the icon means mapping is running.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly System.Drawing.Icon _idleIcon, _mappingIcon;
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
        _idleIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        _mappingIcon = WithStatusDot(_idleIcon);
        _icon = new Forms.NotifyIcon
        {
            Icon = _idleIcon,
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
        _icon.Text = $"TSCC_WASD · {state}"; // NotifyIcon text is limited to 63 characters.
        _icon.Icon = _viewModel.IsRunning ? _mappingIcon : _idleIcon;
    }

    private static System.Drawing.Icon WithStatusDot(System.Drawing.Icon source)
    {
        using var bitmap = source.ToBitmap();
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int size = bitmap.Width / 2, x = bitmap.Width - size, y = bitmap.Height - size;
            using var outline = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(32, 32, 32));
            using var fill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(52, 199, 89));
            g.FillEllipse(outline, x, y, size, size);
            g.FillEllipse(fill, x + 2, y + 2, size - 4, size - 4);
        }
        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var borrowed = System.Drawing.Icon.FromHandle(handle);
            return (System.Drawing.Icon)borrowed.Clone();
        }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    public void ShowBalloon(string text) => _icon.ShowBalloonTip(3000, "TSCC_WASD", text, Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _icon.Visible = false;
        _icon.Dispose();
        _idleIcon.Dispose();
        _mappingIcon.Dispose();
    }
}
