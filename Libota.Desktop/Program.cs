using Avalonia;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;

namespace Libota.Desktop;

public class Program
{
    public static void Main(string[] args) =>
        AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .UseManagedSystemDialogs()
            .StartWithClassicDesktopLifetime(args);
}