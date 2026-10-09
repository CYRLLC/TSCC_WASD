using System.Windows;
using TSCC_WASD.App.ViewModels;
using TSCC_WASD.Core;
using TSCC_WASD.Core.Services;

namespace TSCC_WASD.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = L.T($"版本 {UpdateChecker.CurrentVersion.ToString(3)} 預覽版", $"Version {UpdateChecker.CurrentVersion.ToString(3)} preview");
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url }) MainViewModel.OpenUrl(url);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
