using System;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Users;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Security
{
    public class LoginScreen : ReactiveUserControl<LoginScreenViewModel>
    {
        public LoginScreen()
        {
            InitializeComponent();

            DataContext = Locator.Current.GetService<LoginScreenViewModel>();
            this.WhenActivated(disposables =>
            {
                if (ViewModel == null) return;
                ViewModel.Password = string.Empty;

                ViewModel.NotifyAuthenticationResult.RegisterHandler(DisplayAuthenticationResult);
                ViewModel.Login.Subscribe(session =>
                {
                    if (session is { IsAuthenticated: true })
                        ViewModel.HostScreen.Router.Navigate.Execute(Locator.Current
                            .GetService<DashboardViewModel>()!);
                });

                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private static void DisplayAuthenticationResult(InteractionContext<UserSession?, Unit> interaction)
        {
            var notificationManager = Locator.Current.GetService<INotificationManager>();
            INotification? notification;
            if (interaction.Input is { IsAuthenticated: true })
            {
                notification = new Avalonia.Controls.Notifications.Notification("Login Success", $"Welcome {interaction.Input.UserName}",
                    NotificationType.Success, TimeSpan.FromSeconds(5));
            }
            else
            {
                notification =
                    new Avalonia.Controls.Notifications.Notification("Login Failure", "Login failed",
                        NotificationType.Error, TimeSpan.FromSeconds(5));
            }

            notificationManager?.Show(notification);
            interaction.SetOutput(Unit.Default);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var userNameField = this.FindControl<TextBox>("UserNameField");
            userNameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
        }
    }
}