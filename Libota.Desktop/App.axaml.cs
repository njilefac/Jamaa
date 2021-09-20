using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.Views.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop
{
    public class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            
            Messages.Culture = CultureInfo.CurrentUICulture;
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            desktop.MainWindow = new MainWindow
            {
                DataContext = Locator.Current.GetService<MainWindowViewModel>(),
                WindowState = WindowState.Maximized,
            };
        }
    }
}