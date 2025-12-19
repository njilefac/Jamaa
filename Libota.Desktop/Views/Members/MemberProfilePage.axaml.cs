using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

public partial class MemberProfilePage : UserControl
{
    public MemberProfilePage(MemberProfileViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new MemberProfileViewModel? DataContext { get; set; }
}