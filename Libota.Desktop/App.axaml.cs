using Avalonia.Markup.Xaml;

namespace Libota.Desktop;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}