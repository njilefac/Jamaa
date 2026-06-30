using Avalonia;
using Avalonia.Controls;

namespace Jamaa.Desktop.Shared.Controls;

public class FiniteMeasureDecorator : Decorator
{
    private static readonly Size FallbackSize = new(1024, 768);

    protected override Size MeasureOverride(Size availableSize)
    {
        var constrainedSize = Constrain(availableSize);
        Child?.Measure(constrainedSize);
        return constrainedSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var constrainedSize = Constrain(finalSize);
        Child?.Arrange(new Rect(constrainedSize));
        return constrainedSize;
    }

    private Size Constrain(Size size)
    {
        var topLevelSize = TopLevel.GetTopLevel(this)?.ClientSize ?? FallbackSize;

        return new Size(
            ConstrainDimension(size.Width, topLevelSize.Width, FallbackSize.Width),
            ConstrainDimension(size.Height, topLevelSize.Height, FallbackSize.Height));
    }

    private static double ConstrainDimension(double value, double topLevelValue, double fallbackValue)
    {
        if (double.IsFinite(value) && value > 0)
            return value;

        if (double.IsFinite(topLevelValue) && topLevelValue > 0)
            return topLevelValue;

        return fallbackValue;
    }
}
