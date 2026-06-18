using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using Jamaa.Desktop.Services.Notifications;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Shared;

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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<VisualLayerManager>("PART_VisualLayerManager") is { } visualLayerManager)
        {
            visualLayerManager.EnableOverlayLayer = true;
            return;
        }

        if (e.NameScope.Find<AppSurface>("PART_AppSurface") is not { } appSurface)
            return;

        if (appSurface.Parent is VisualLayerManager existingLayerManager)
        {
            existingLayerManager.EnableOverlayLayer = true;
            return;
        }

        if (appSurface.Parent is not Panel parentPanel)
            return;

        var appSurfaceIndex = parentPanel.Children.IndexOf(appSurface);
        if (appSurfaceIndex < 0)
            return;

        parentPanel.Children.RemoveAt(appSurfaceIndex);
        var layerManager = new VisualLayerManager
        {
            EnableOverlayLayer = true,
            Child = appSurface
        };
        parentPanel.Children.Insert(appSurfaceIndex, layerManager);
    }
}