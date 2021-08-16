using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.Views;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Locator.Current.GetService<MainWindowViewModel>(),
                    WindowState = WindowState.Maximized,
                };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}