using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Views.Members;

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