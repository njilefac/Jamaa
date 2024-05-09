using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Setup;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using ReactiveUI;

namespace Libota.Desktop.Views.Setup;

public partial class CreateOrganisationScreen : ReactiveUserControl<CreateOrganisationViewModel>
{
    public CreateOrganisationScreen(CreateOrganisationViewModel createOrganisationViewModel,
        ISetupService setupService, CreateSuperUserViewModel createSuperUserViewModel,
        LoginScreenViewModel loginScreenViewModel)
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            DataContext = createOrganisationViewModel;
            ViewModel?.CreateOrganisation.Subscribe(organisationWasCreated =>
            {
                if (!organisationWasCreated) return;

                var superUser = setupService.GetSuperUser().Result;
                IRoutableViewModel nextViewModel = superUser == null ? createSuperUserViewModel : loginScreenViewModel;
                ViewModel.HostScreen.Router.Navigate.Execute(nextViewModel);
            });

            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var nameField = this.FindControl<TextBox>("OrganisationNameField");
        nameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }
}