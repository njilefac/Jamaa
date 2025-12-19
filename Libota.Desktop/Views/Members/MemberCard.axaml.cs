using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;

namespace Libota.Desktop.Views.Members;

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