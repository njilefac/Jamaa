using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberManagementScreen : UserControl
{
    public MemberManagementScreen()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}