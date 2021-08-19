using System;
using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.ViewModels.Security;
using Club.Station.Desktop.ViewModels.Shared;
using Club.Station.Desktop.ViewModels.Users;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Security
{
    public partial class LoginScreenView : ReactiveUserControl<LoginScreenViewModel>
    {
        public LoginScreenView()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetService<LoginScreenViewModel>();
            this.WhenActivated(disposables =>
            {
                if (ViewModel == null) return;
                
                ViewModel.UserName = string.Empty;
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
        }
    }
}