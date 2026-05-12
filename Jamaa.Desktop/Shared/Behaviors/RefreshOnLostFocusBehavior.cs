using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Jamaa.Desktop.Accounting;

namespace Jamaa.Desktop.Shared.Behaviors;

/// <summary>
/// A behavior that triggers a refresh of the OpeningBalance formatting when the TextBox loses focus.
/// It calls ForceFormatOpeningBalance on the AccountItemViewModel if it's the DataContext.
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
        if (AssociatedObject?.DataContext is OpeningBalanceItemViewModel vm)
        {
            vm.ForceFormatOpeningBalance();
        }
    }
}
