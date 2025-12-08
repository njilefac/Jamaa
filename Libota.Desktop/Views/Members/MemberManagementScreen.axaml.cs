using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberManagementScreen : UserControl, IViewFor<MembersManagementScreenViewModel>
{
    public MemberManagementScreen(MembersManagementScreenViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new MembersManagementScreenViewModel? DataContext { get; set; }
}