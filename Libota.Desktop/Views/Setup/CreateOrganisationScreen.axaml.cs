using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Setup;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Setup
{
    public class CreateOrganisationScreen : ReactiveUserControl<CreateOrganisationViewModel>
    {
        public CreateOrganisationScreen()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<CreateOrganisationViewModel>();
                ViewModel?.CreateOrganisation.Subscribe(wasOrganisationCreated =>
                {
                    if (!wasOrganisationCreated) return;
                    var setupService = Locator.Current.GetService<ISetupService>();

                    IRoutableViewModel? nextViewModel = setupService?.GetSuperUser().Result == null
                        ? Locator.Current.GetService<CreateSuperUserViewModel>()
                        : Locator.Current.GetService<LoginScreenViewModel>();
                    
                    if (nextViewModel != null)
                        ViewModel.HostScreen.Router.Navigate.Execute(nextViewModel!);
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
}