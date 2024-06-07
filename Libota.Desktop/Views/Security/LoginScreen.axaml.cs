using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Security;

[SingleInstanceView]
public partial class LoginScreen : ReactiveUserControl<LoginScreenViewModel>
{
    public LoginScreen()
    {
        InitializeComponent();

        var vm = Locator.Current.GetService<LoginScreenViewModel>();
        var dashboardVm = Locator.Current.GetService<DashboardViewModel>();

        DataContext = vm;
        this.WhenActivated(disposables =>
        {
            if (ViewModel == null) return;
            ViewModel.Password = string.Empty;

            ViewModel.Login.Subscribe(session =>
            {
                if (session is { IsAuthenticated: true })
                    ViewModel.HostScreen.Router.Navigate.Execute(dashboardVm ?? throw new InvalidOperationException());
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