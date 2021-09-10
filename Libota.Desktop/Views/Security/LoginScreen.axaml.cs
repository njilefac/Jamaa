using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Security
{
    public class LoginScreenView : ReactiveUserControl<LoginScreenViewModel>
    {
        public LoginScreenView()
        {
            InitializeComponent();
            
            DataContext = Locator.Current.GetService<LoginScreenViewModel>();
            this.WhenActivated(disposables =>
            {
                if (ViewModel == null) return;
                ViewModel.Password = string.Empty;

                ViewModel.Login.Subscribe(session =>
                {
                    if(session is { IsAuthenticated: true })
                        ViewModel.HostScreen.Router.Navigate.Execute(Locator.Current
                            .GetService<DashboardViewModel>()!);
                });

                Disposable.Create(() => { }).DisposeWith(disposables);
            });
            
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var userNameField = this.FindControl<TextBox>("UserNameField");
            userNameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
        }
    }
}