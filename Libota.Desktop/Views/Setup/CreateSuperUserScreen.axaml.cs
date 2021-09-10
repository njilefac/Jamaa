using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Setup
{
    public class CreateSuperUserScreen : ReactiveUserControl<CreateSuperUserViewModel>
    {
        public CreateSuperUserScreen()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<CreateSuperUserViewModel>();

                ViewModel?.CreateAccount.Subscribe(session =>
                {
                    if (session is { IsAuthenticated: true })
                        ViewModel.HostScreen.Router.Navigate.Execute(Locator.Current
                            .GetService<DashboardViewModel>()!);
                    else
                    {
                        ViewModel.HostScreen.Router.Navigate.Execute(Locator.Current
                            .GetService<LoginScreenViewModel>()!);
                    }
                });

                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var nameField = this.FindControl<TextBox>("UserNameField");
            nameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
        }
    }
}