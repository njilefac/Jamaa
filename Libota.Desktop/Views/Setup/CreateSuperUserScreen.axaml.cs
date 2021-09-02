using System;
using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
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