using System;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Shared
{
    [UsedImplicitly]
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(IUserSessionService userSessionService)
        {
            userSessionService.UserSessions.Subscribe(x =>
            {
                ApplicationTitle = x is { IsAuthenticated: true }
                    ? $"{ApplicationName} -  ({x.Organisation?.Name})"
                    : ApplicationName;
            });
        }

        [ObservableProperty]
        private string? _applicationTitle = ApplicationName;
        private const string ApplicationName = "Libota Desktop";
    }
}