using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Jamaa.Desktop.Services;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashViewModel = new SplashViewModel();
            var splashWindow = new SplashWindow { DataContext = splashViewModel };
            desktop.MainWindow = splashWindow;

            desktop.Exit += (_, _) =>
            {
                // Trigger shutdown but don't wait for it to complete on the UI thread
                _ = InitializationService.ShutdownAsync();
            };

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var mainWindow = await InitializationService.InitializeAsync(desktop);
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                splashWindow.Close();
                splashViewModel.Dispose();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}