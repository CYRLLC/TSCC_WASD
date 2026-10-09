using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Xunit;
using YnyrWASD.App;
using YnyrWASD.App.ViewModels;
using YnyrWASD.Core.Services;

namespace YnyrWASD.Tests;

public class DesktopTests
{
    [Fact]
    public async Task EditorCommandsAndWindowBindingsWorkWithoutDrivers()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            string dir = Path.Combine(Path.GetTempPath(), "YnyrWASD-tests", Guid.NewGuid().ToString("N"));
            var errors = new StringWriter();
            using var listener = new TextWriterTraceListener(errors);
            PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
            MainViewModel? vm = null;
            try
            {
                var store = new ProfileStore(Path.Combine(dir, "profiles.json"));
                vm = new MainViewModel(store);
                Assert.Single(vm.Profiles);
                vm.NewCommand.Execute(null);
                Assert.Equal(2, vm.Profiles.Count);
                vm.SelectedProfile!.Name = "測試設定";
                vm.SelectedProfile.DeadZone = 0.2;
                vm.SelectedProfile.InputType = YnyrWASD.Core.Models.InputDeviceType.Switch2ProUsb;
                vm.SaveCommand.Execute(null);
                Assert.Equal("測試設定", store.LoadProfiles()[1].Name);
                Assert.Equal(0.2, store.LoadProfiles()[1].DeadZone);
                Assert.Equal(YnyrWASD.Core.Models.InputDeviceType.Switch2ProUsb, store.LoadProfiles()[1].InputType);
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
                surface.Measure(new Size(980, 700));
                surface.Arrange(new Rect(0, 0, 980, 700));
                surface.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                string? screenshotDir = Environment.GetEnvironmentVariable("YNYRWASD_SCREENSHOTS");
                if (!string.IsNullOrEmpty(screenshotDir))
                {
                    Directory.CreateDirectory(screenshotDir);
                    var bitmap = new RenderTargetBitmap(980, 700, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(surface);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(screenshotDir, "editor.png"));
                    encoder.Save(stream);
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
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }
}
