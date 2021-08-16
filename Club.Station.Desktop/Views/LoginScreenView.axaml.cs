using System;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views
{
    public partial class LoginScreenView : ReactiveUserControl<LoginScreenViewModel>
    {
        public LoginScreenView()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetService<LoginScreenViewModel>();
            this.WhenActivated(disposables =>
            {
                ViewModel?.Login.Subscribe(session =>
                {
                    if(session is { Authenticated: true })
                        ViewModel.HostScreen.Router.Navigate.Execute(Locator.Current
                        .GetService<UserManagementViewModel>()!);
                });
            });
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}