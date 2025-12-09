using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberRegistrationDialog : Window
{
    public MemberRegistrationDialog(MemberRegistrationDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        FirstNameField?.Focus();
    }

    public new MemberRegistrationDialogViewModel? DataContext { get; set; }
}