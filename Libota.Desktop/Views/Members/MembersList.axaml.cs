using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MembersList : UserControl, IViewFor<MembersListViewModel>
{
    public MembersList()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new MembersListViewModel? DataContext { get; set; }
}