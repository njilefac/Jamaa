using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Libota.Desktop.Infrastructure.Windows;

public class TopLevelProvider : ITopLevelProvider
{
    public TopLevel? Current =>
        (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}