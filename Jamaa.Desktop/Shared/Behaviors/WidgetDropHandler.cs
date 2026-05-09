using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Jamaa.Desktop.Dashboard;

namespace Jamaa.Desktop.Shared.Behaviors;

public class WidgetDropHandler : IDropHandler
{
    public void Enter(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        var isValid = ValidateDrop(sourceContext, targetContext);
        e.DragEffects = isValid ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;

        if (targetContext is WidgetViewModelBase target)
        {
            target.IsDraggingOver = true;
            target.IsValidDrop = isValid;
        }
    }

    public void Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        var isValid = ValidateDrop(sourceContext, targetContext);
        e.DragEffects = isValid ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;

        if (targetContext is not WidgetViewModelBase target) return;
        target.IsDraggingOver = true;
        target.IsValidDrop = isValid;
    }

    public void Drop(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        if (targetContext is WidgetViewModelBase target) target.IsDraggingOver = false;

        if (ValidateDrop(sourceContext, targetContext) &&
            sourceContext is WidgetViewModelBase dragged &&
            targetContext is WidgetViewModelBase targetWidget &&
            dragged.ParentViewModel is { } vm)
        {
            var tempRow = dragged.Row;
            var tempCol = dragged.Column;

            dragged.Row = targetWidget.Row;
            dragged.Column = targetWidget.Column;

            targetWidget.Row = tempRow;
            targetWidget.Column = tempCol;

            _ = vm.SaveLayout();
        }

        e.Handled = true;
    }

    public void Leave(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: WidgetViewModelBase target }) target.IsDraggingOver = false;
    }

    public bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        return ValidateDrop(sourceContext, targetContext);
    }

    public bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (ValidateDrop(sourceContext, targetContext) &&
            sourceContext is WidgetViewModelBase dragged &&
            targetContext is WidgetViewModelBase target &&
            dragged.ParentViewModel is { } vm)
        {
            var tempRow = dragged.Row;
            var tempCol = dragged.Column;

            dragged.Row = target.Row;
            dragged.Column = target.Column;

            target.Row = tempRow;
            target.Column = tempCol;

            _ = vm.SaveLayout();
            return true;
        }

        return false;
    }

    public void Cancel(object? sender, RoutedEventArgs e)
    {
    }

    private bool ValidateDrop(object? sourceContext, object? targetContext)
    {
        if (sourceContext is not WidgetViewModelBase dragged ||
            targetContext is not WidgetViewModelBase target) return false;
        if (dragged == target) return false;
        if (dragged.ParentViewModel is not { } vm) return false;

        // In Bento Box, widgets can only be dropped into slots that match their allowed size
        // AND the widget currently in that slot (target) must be able to move to the dragged widget's current slot.

        // 1. Can dragged widget go to target's slot?
        var targetBoxSize = target.Column == 1 ? BoxSize.Wide : BoxSize.Small;
        if (dragged.AllowedBoxSize != targetBoxSize) return false;

        // 2. Can target widget go to dragged widget's slot?
        var draggedBoxSize = dragged.Column == 1 ? BoxSize.Wide : BoxSize.Small;
        return target.AllowedBoxSize == draggedBoxSize;
    }
}