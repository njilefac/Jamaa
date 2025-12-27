using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Members.Pages;

public partial class MembersOverviewPage : UserControl
{
    public MembersOverviewPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}