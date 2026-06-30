using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Jamaa.Desktop.Shared.Behaviors;

public class FocusOnAttachedBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null)
            return;

        AssociatedObject.AttachedToVisualTree += FocusAssociatedObject;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.AttachedToVisualTree -= FocusAssociatedObject;

        base.OnDetaching();
    }

    private static void FocusAssociatedObject(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        Dispatcher.UIThread.Post(() => textBox.Focus(NavigationMethod.Tab, KeyModifiers.None), DispatcherPriority.Loaded);
    }
}
