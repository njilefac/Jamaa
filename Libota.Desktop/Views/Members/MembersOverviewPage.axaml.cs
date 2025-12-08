using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Domain.Organisation.Requests;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MembersOverviewPage : UserControl, IViewFor<MembersOverviewPageViewModel>
{
    private readonly IViewFor<MemberRegistrationDialogViewModel> _memberRegistrationDialog;
    private readonly IViewFor<MainWindowViewModel> _mainWindow;

    public MembersOverviewPage(MembersOverviewPageViewModel viewModel,
        IViewFor<MemberRegistrationDialogViewModel> memberRegistrationDialog,
        IViewFor<MainWindowViewModel> mainWindow)
    {
        _memberRegistrationDialog = memberRegistrationDialog;
        _mainWindow = mainWindow;
        InitializeComponent();

        DataContext = viewModel;

        viewModel.ShowRegistrationPrompt.RegisterHandler(ctx =>
        {
            var request = ShowMemberRegistrationDialog().Result;
            ctx.SetOutput(request);
        });
    }

    private async Task<MemberRegistrationRequest> ShowMemberRegistrationDialog()
    {
        var dialog = _memberRegistrationDialog as Window;
        var result = await dialog?.ShowDialog<MemberRegistrationRequest>(_mainWindow as Window ??
                                                                         throw new InvalidOperationException())!;
        return result;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new MembersOverviewPageViewModel? DataContext { get; set; }
}