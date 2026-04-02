using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Jamaa.Desktop.Dashboard;

namespace Jamaa.Desktop.Shared.Behaviors;

public class WidgetDropHandler : IDropHandler
{
    public void Enter(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        e.DragEffects = ValidateDrop(sourceContext, targetContext) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    public void Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        e.DragEffects = ValidateDrop(sourceContext, targetContext) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    public void Drop(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        if (ValidateDrop(sourceContext, targetContext) && 
            sourceContext is WidgetViewModelBase dragged && 
            targetContext is WidgetViewModelBase target &&
            dragged.ParentViewModel is { } vm)
        {
            var tempRow = dragged.Row;
            var tempCol = dragged.Column;
            var tempRowSpan = dragged.RowSpan;
            var tempColSpan = dragged.ColumnSpan;

            dragged.Row = target.Row;
            dragged.Column = target.Column;
            dragged.RowSpan = target.RowSpan;
            dragged.ColumnSpan = target.ColumnSpan;

            target.Row = tempRow;
            target.Column = tempCol;
            target.RowSpan = tempRowSpan;
            target.ColumnSpan = tempColSpan;

            vm.SaveLayout();
        }
        e.Handled = true;
    }

    public void Leave(object? sender, RoutedEventArgs e) { }
    public bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        throw new System.NotImplementedException();
    }

    public bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        throw new System.NotImplementedException();
    }

    public void Cancel(object? sender, RoutedEventArgs e) { }

    private bool ValidateDrop(object? sourceContext, object? targetContext)
    {
        if (sourceContext is not WidgetViewModelBase dragged || targetContext is not WidgetViewModelBase target) return false;
        if (dragged == target) return false;
        if (dragged.ParentViewModel is not { } vm) return false;

        // Check if dragged widget will fit in target's location
        if (target.Column + dragged.ColumnSpan > vm.MaxColumns || target.Row + dragged.RowSpan > vm.MaxRows) return false;

        // Check if target widget will fit in dragged widget's old location
        return dragged.Column + target.ColumnSpan <= vm.MaxColumns && dragged.Row + target.RowSpan <= vm.MaxRows;
    }
}