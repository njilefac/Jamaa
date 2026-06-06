using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Jamaa.Desktop.Accounting;

namespace Jamaa.Desktop.Shared.Behaviors;

/// <summary>
/// Refreshes the opening-balance display when the TextBox loses focus.
/// </summary>
public class RefreshOnLostFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.LostFocus += OnLostFocus;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.LostFocus -= OnLostFocus;
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject?.DataContext is AccountItemViewModel accountItemViewModel)
        {
            accountItemViewModel.ForceFormatOpeningBalance();
        }
    }
}
