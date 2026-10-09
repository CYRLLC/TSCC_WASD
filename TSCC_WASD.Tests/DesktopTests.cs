using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Xunit;
using TSCC_WASD.App;
using TSCC_WASD.App.ViewModels;
using TSCC_WASD.Core.Services;

namespace TSCC_WASD.Tests;

public class DesktopTests
{
    [Fact]
    public async Task EditorCommandsAndWindowBindingsWorkWithoutDrivers()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            string dir = Path.Combine(Path.GetTempPath(), "TSCC_WASD-tests", Guid.NewGuid().ToString("N"));
            var errors = new StringWriter();
            using var listener = new TextWriterTraceListener(errors);
            PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
            MainViewModel? vm = null;
            try
            {
                var store = new ProfileStore(Path.Combine(dir, "profiles.json"));
                // Screenshot runs pick the UI language; normal runs keep the default.
                if (Environment.GetEnvironmentVariable("YNYRWASD_LANGUAGE") is { Length: > 0 } language)
                    TSCC_WASD.Core.L.Apply(language);
                vm = new MainViewModel(store, new AppSettingsStore(Path.Combine(dir, "settings.json")));
                Assert.Single(vm.Profiles);
                vm.NewCommand.Execute(null);
                Assert.Equal(2, vm.Profiles.Count);
                string profileName = TSCC_WASD.Core.L.T("測試設定", "Test profile");
                vm.SelectedProfile!.Name = profileName;
                vm.SelectedProfile.DeadZone = 0.2;
                vm.SelectedProfile.InputType = TSCC_WASD.Core.Models.InputDeviceType.Switch2ProUsb;
                vm.SaveCommand.Execute(null);
                Assert.Equal(profileName, store.LoadProfiles()[1].Name);
                Assert.Equal(0.2, store.LoadProfiles()[1].DeadZone);
                Assert.Equal(TSCC_WASD.Core.Models.InputDeviceType.Switch2ProUsb, store.LoadProfiles()[1].InputType);
                vm.ReloadCommand.Execute(null);
                vm.DeleteCommand.Execute(null);
                Assert.Single(vm.Profiles);
                Assert.False(vm.DeleteCommand.CanExecute(null));
                Assert.True(vm.StartCommand.CanExecute(null));
                Assert.False(vm.StopCommand.CanExecute(null));

                var window = new MainWindow(vm);
                var content = (FrameworkElement)window.Content;
                window.Content = null;
                var surface = new System.Windows.Controls.Border
                {
                    Child = content, Background = window.Background,
                    Resources = window.Resources, DataContext = vm
                };
                System.Windows.Documents.TextElement.SetForeground(surface, window.Foreground);
                surface.Measure(new Size(980, 780));
                surface.Arrange(new Rect(0, 0, 980, 780));
                surface.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                string? screenshotDir = Environment.GetEnvironmentVariable("YNYRWASD_SCREENSHOTS");
                if (!string.IsNullOrEmpty(screenshotDir))
                {
                    Directory.CreateDirectory(screenshotDir);
                    var bitmap = new RenderTargetBitmap(980, 780, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(surface);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(Path.Combine(screenshotDir, "editor.png"))) encoder.Save(stream);

                    // Second shot with every section expanded, to review all controls.
                    foreach (var expander in FindAll<System.Windows.Controls.Expander>(surface)) expander.IsExpanded = true;
                    var scroller = FindAll<System.Windows.Controls.ScrollViewer>(surface).First();
                    scroller.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled;
                    surface.Measure(new Size(980, 1600));
                    surface.Arrange(new Rect(0, 0, 980, 1600));
                    surface.UpdateLayout();
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    var expanded = new RenderTargetBitmap(980, 1600, 96, 96, PixelFormats.Pbgra32);
                    expanded.Render(surface);
                    var expandedEncoder = new PngBitmapEncoder();
                    expandedEncoder.Frames.Add(BitmapFrame.Create(expanded));
                    using (var stream = File.Create(Path.Combine(screenshotDir, "editor-expanded.png"))) expandedEncoder.Save(stream);
                }
                Assert.Equal("", errors.ToString());
                window.Close();
                completion.TrySetResult();
            }
            catch (Exception ex) { completion.TrySetException(ex); }
            finally
            {
                vm?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        // Generous: a cold GitHub runner once needed over 20 s for the first WPF render.
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(60));
    }

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in FindAll<T>(child)) yield return nested;
        }
    }

    [Fact]
    public async Task CalibrationWizardLoadsAndRenders()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            CalibrationWindow? window = null;
            try
            {
                // Without an NS2 Pro attached the wizard waits on its first step; it must still load.
                window = new CalibrationWindow();
                var rangePanel = (FrameworkElement)window.FindName("RangePanel");
                var content = (FrameworkElement)window.Content;
                window.Content = null;
                var surface = new System.Windows.Controls.Border { Child = content, Background = window.Background, Resources = window.Resources };
                System.Windows.Documents.TextElement.SetForeground(surface, window.Foreground);
                surface.Measure(new Size(560, double.PositiveInfinity));
                surface.Arrange(new Rect(new Point(), surface.DesiredSize));
                surface.UpdateLayout();
                Assert.True(surface.ActualHeight > 100);
                string? screenshotDir = Environment.GetEnvironmentVariable("YNYRWASD_SCREENSHOTS");
                if (!string.IsNullOrEmpty(screenshotDir))
                {
                    // Show the second step so the screenshot includes the progress bars.
                    rangePanel.Visibility = Visibility.Visible;
                    surface.Measure(new Size(560, double.PositiveInfinity));
                    surface.Arrange(new Rect(new Point(), surface.DesiredSize));
                    surface.UpdateLayout();
                    Directory.CreateDirectory(screenshotDir);
                    var bitmap = new RenderTargetBitmap((int)surface.ActualWidth, (int)surface.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(surface);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(screenshotDir, "calibration.png"));
                    encoder.Save(stream);
                }
                window.Close();
                completion.TrySetResult();
            }
            catch (Exception ex) { completion.TrySetException(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        // Generous: a cold GitHub runner once needed over 20 s for the first WPF render.
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(60));
    }
}
