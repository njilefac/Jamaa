using Avalonia.Controls;
using Avalonia.Input;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCarousel : UserControl
{
    public MemberCarousel()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var carousel = this.FindControl<Carousel>("MemberCarouselView");
        if (carousel == null) return;

        switch (e.Key)
        {
            case Key.Left:
            case Key.Up:
            case Key.PageUp:
                carousel.Previous();
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Down:
            case Key.PageDown:
                carousel.Next();
                e.Handled = true;
                break;
        }
    }
}
