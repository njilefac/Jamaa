using Avalonia;

namespace Jamaa.Desktop;

public partial class Program
{
    public static void Main(string[] args)
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args);
    }
}