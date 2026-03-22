using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Libota.Desktop.Services;
using Libota.Desktop.Shared;

namespace Libota.Desktop;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashWindow = new SplashWindow();
            desktop.MainWindow = splashWindow;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var mainWindow = await InitializationService.InitializeAsync(desktop);
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                splashWindow.Close();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}