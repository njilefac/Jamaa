using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Jamaa.Desktop.Dashboard;

public partial class WidgetView : UserControl
{
    private static readonly Cursor DragCursor = new(StandardCursorType.DragMove);

    private WidgetViewModelBase? _currentDropTarget;
    private WidgetViewModelBase? _draggedWidget;
    private bool _isDragging;
    private Point _dragStartPoint;

    public WidgetView()
    {
        InitializeComponent();
        var widgetBorder = this.FindControl<Border>("WidgetBorder");
        if (widgetBorder is null)
            return;

        widgetBorder.PointerPressed += OnPointerPressed;
        widgetBorder.PointerMoved += OnPointerMoved;
        widgetBorder.PointerReleased += OnPointerReleased;
        widgetBorder.PointerCaptureLost += OnPointerCaptureLost;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || IsInteractiveChild(e.Source))
            return;

        if (DataContext is not WidgetViewModelBase widget)
            return;

        _dragStartPoint = e.GetPosition(this);
        _draggedWidget = widget;
        e.Pointer.Capture(sender as IInputElement);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (!_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < 6 && Math.Abs(currentPoint.Y - _dragStartPoint.Y) < 6)
                return;

            _isDragging = true;
            Cursor = DragCursor;
        }

        UpdateDropTarget(e);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
            TrySwapWithHitWidget(e);

        ClearDropTarget();
        e.Pointer.Capture(null);
        _draggedWidget = null;
        _isDragging = false;
        Cursor = null;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ClearDropTarget();
        _draggedWidget = null;
        _isDragging = false;
        Cursor = null;
    }

    private static void SwapWidgets(WidgetViewModelBase dragged, WidgetViewModelBase target)
    {
        var tempRow = dragged.Row;
        var tempCol = dragged.Column;

        dragged.Row = target.Row;
        dragged.Column = target.Column;

        target.Row = tempRow;
        target.Column = tempCol;

        if (dragged.ParentViewModel is { } vm)
            _ = vm.SaveLayout();
    }

    private void TrySwapWithHitWidget(PointerReleasedEventArgs e)
    {
        if (_draggedWidget is null)
            return;

        var target = _currentDropTarget ?? GetDropTarget(e);

        if (ValidateDrop(_draggedWidget, target) && target is not null)
            SwapWidgets(_draggedWidget, target);
    }

    private void UpdateDropTarget(PointerEventArgs e)
    {
        if (_draggedWidget is null)
            return;

        var target = GetDropTarget(e);
        if (ReferenceEquals(target, _currentDropTarget))
        {
            if (target is not null)
                target.IsValidDrop = ValidateDrop(_draggedWidget, target);

            return;
        }

        ClearDropTarget();

        _currentDropTarget = target;
        if (_currentDropTarget is null)
            return;

        _currentDropTarget.IsDraggingOver = true;
        _currentDropTarget.IsValidDrop = ValidateDrop(_draggedWidget, _currentDropTarget);
    }

    private WidgetViewModelBase? GetDropTarget(PointerEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var hit = topLevel?.InputHitTest(e.GetPosition(topLevel));
        var targetView = (hit as Visual)?.FindAncestorOfType<WidgetView>(includeSelf: true);
        return targetView?.DataContext as WidgetViewModelBase;
    }

    private void ClearDropTarget()
    {
        if (_currentDropTarget is null)
            return;

        _currentDropTarget.IsDraggingOver = false;
        _currentDropTarget.IsValidDrop = false;
        _currentDropTarget = null;
    }

    private static bool ValidateDrop(WidgetViewModelBase? dragged, WidgetViewModelBase? target)
    {
        if (dragged is null || target is null) return false;
        if (dragged == target) return false;
        if (dragged.ParentViewModel is null) return false;

        var targetBoxSize = target.Column == 1 ? BoxSize.Wide : BoxSize.Small;
        if (dragged.AllowedBoxSize != targetBoxSize) return false;

        var draggedBoxSize = dragged.Column == 1 ? BoxSize.Wide : BoxSize.Small;
        return target.AllowedBoxSize == draggedBoxSize;
    }

    private static bool IsInteractiveChild(object? source)
    {
        return source is Visual visual &&
               (visual is Button or ListBox or MenuItem ||
                visual.FindAncestorOfType<Button>() is not null ||
                visual.FindAncestorOfType<ListBox>() is not null ||
                visual.FindAncestorOfType<MenuItem>() is not null);
    }
}
