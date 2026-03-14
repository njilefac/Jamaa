using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using JetBrains.Annotations;
using Libota.Desktop.Services.Notifications;

namespace Libota.Desktop.Shared;

[UsedImplicitly]
public partial class Shell : AppWindow
{

    public Shell()
    {
        InitializeComponent();
    }

    public Shell(AvaloniaNotificationService notificationService) : this()
    {
        notificationService.SetNotificationManager(new WindowNotificationManager(this));

#if DEBUG
        //this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}