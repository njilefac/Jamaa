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

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class CreateSuperUserScreen : ReactiveUserControl<CreateSuperUserViewModel>
{
    public CreateSuperUserScreen()
    {
        InitializeComponent();
        
        var vm = Locator.Current.GetService<CreateSuperUserViewModel>();
        var dashBoardVm = Locator.Current.GetService<DashboardViewModel>();
        var loginVm = Locator.Current.GetService<LoginScreenViewModel>();

        this.WhenActivated(disposables =>
        {
            ViewModel = vm;

            ViewModel?.CreateAccount.Subscribe(session =>
            {
                if (session is { IsAuthenticated: true })
                    ViewModel.HostScreen.Router.Navigate.Execute(dashBoardVm ?? throw new InvalidOperationException());
                else
                    ViewModel.HostScreen.Router.Navigate.Execute(loginVm ?? throw new InvalidOperationException());
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