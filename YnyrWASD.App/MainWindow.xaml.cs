using System.ComponentModel;
using System.Windows;
using YnyrWASD.App.ViewModels;

namespace YnyrWASD.App;

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
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_readyToClose) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        IsEnabled = false;
        try { await _viewModel.DisposeAsync(); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "清理控制器時發生錯誤");
        }
        finally
        {
            _readyToClose = true;
            Close();
        }
    }
}
