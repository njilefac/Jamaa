using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;

namespace Libota.Desktop.Members.Components;

public partial class MemberCard : Card
{
    public MemberCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}