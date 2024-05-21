using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Domain.Organisation.Requests;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MembersOverviewPage : ReactiveUserControl<MembersOverviewPageViewModel>
{
    public MembersOverviewPage()
    {
        InitializeComponent();

        ViewModel = Locator.Current.GetService<MembersOverviewPageViewModel>();
        this.WhenActivated(disposables =>
        {
            this.BindInteraction(ViewModel, vm => vm.ShowRegistrationPrompt, ShowMemberRegistrationDialog);
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
        
    }

    private static async Task ShowMemberRegistrationDialog(IInteractionContext<Unit, MemberRegistrationRequest> interaction)
    {
        var dialog = Locator.Current.GetService<IViewFor<MemberRegistrationDialogViewModel>>() as Window;
        var result = await dialog?.ShowDialog<MemberRegistrationRequest>(Locator.Current.GetService<IViewFor<MainWindowViewModel>>() as Window ??
                                                                         throw new InvalidOperationException())!;
        interaction.SetOutput(result);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}