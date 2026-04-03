using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Dashboard;

public partial class WidgetView : UserControl
{
    public WidgetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWidgetResizeCompleted(object? sender, VectorEventArgs e)
    {
        if (sender is Thumb thumb && 
            thumb.DataContext is WidgetViewModelBase widget &&
            widget.ParentViewModel is DashboardViewModel dashboard)
        {
            var container = thumb.Parent as Grid;
            if (container == null) return;

            double cellWidth = container.Bounds.Width / widget.ColumnSpan;
            double cellHeight = container.Bounds.Height / widget.RowSpan;

            int columnsToChange = (int)Math.Round(e.Vector.X / cellWidth);
            int rowsToChange = (int)Math.Round(e.Vector.Y / cellHeight);

            bool layoutChanged = false;

            if (widget.IsMultiColumn && columnsToChange != 0)
            {
                int newSpan = widget.ColumnSpan + columnsToChange;
                if (newSpan >= widget.MinColumnSpan && (widget.Column + newSpan) <= dashboard.MaxColumns)
                {
                    if (IsAreaClear(dashboard, widget.Row, widget.Column, widget.RowSpan, newSpan, widget))
                    {
                        widget.ColumnSpan = newSpan;
                        layoutChanged = true;
                    }
                }
            }

            if (widget.IsMultiRow && rowsToChange != 0)
            {
                int newSpan = widget.RowSpan + rowsToChange;
                if (newSpan >= widget.MinRowSpan && (widget.Row + newSpan) <= dashboard.MaxRows)
                {
                    if (IsAreaClear(dashboard, widget.Row, widget.Column, newSpan, widget.ColumnSpan, widget))
                    {
                        widget.RowSpan = newSpan;
                        layoutChanged = true;
                    }
                }
            }

            if (layoutChanged) dashboard.SaveLayout();
        }
    }

    private bool IsAreaClear(DashboardViewModel dashboard, int targetRow, int targetCol, int targetRowSpan, int targetColSpan, WidgetViewModelBase currentWidget)
    {
        for (int r = targetRow; r < targetRow + targetRowSpan; r++)
        {
            for (int c = targetCol; c < targetCol + targetColSpan; c++)
            {
                var occupyingWidget = dashboard.ActiveWidgets.FirstOrDefault(w => 
                    w != currentWidget && 
                    w.Row <= r && r < w.Row + w.RowSpan && 
                    w.Column <= c && c < w.Column + w.ColumnSpan);

                if (occupyingWidget != null && occupyingWidget is not EmptyCellViewModel)
                {
                    return false;
                }
            }
        }
        return true;
    }
}
