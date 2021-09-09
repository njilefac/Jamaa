using System;
using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
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
                ViewModel?.CreateOrganisation.Subscribe(created =>
                {
                    if (created)
                        ViewModel.HostScreen.Router.Navigate.Execute(
                            Locator.Current.GetService<CreateSuperUserViewModel>()!);
                });

                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}