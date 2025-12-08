using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;

namespace Libota.Desktop.ViewModels.Shared
{
    [UsedImplicitly]
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ISetupService _setupService;
        private readonly CreateOrganisationViewModel _createOrganisationViewModel;
        private readonly CreateSuperUserViewModel _createSuperUserViewModel;
        private readonly LoginScreenViewModel _loginScreenViewModel;

        public MainWindowViewModel(
            ISetupService setupService,
            IUserSessionService userSessionService,
            CreateOrganisationViewModel createOrganisationViewModel,
            CreateSuperUserViewModel createSuperUserViewModel,
            LoginScreenViewModel loginScreenViewModel)
        {
            _setupService = setupService;
            _createOrganisationViewModel = createOrganisationViewModel;
            _createSuperUserViewModel = createSuperUserViewModel;
            _loginScreenViewModel = loginScreenViewModel;
            CurrentView = GetStartupViewModel();
            
            userSessionService.UserSessions.Subscribe(x =>
            {
                ApplicationTitle = x is { IsAuthenticated: true }
                    ? $"{ApplicationName} -  ({x.Organisation?.Name})"
                    : ApplicationName;
            });
        }

        private ObservableObject GetStartupViewModel()
        {
            var existingOrganisations = _setupService.ListOrganisations().Result;

            if (!existingOrganisations.Any())
                return _createOrganisationViewModel;

            var superUser = _setupService.GetSuperUser().Result;
            if (superUser == null)
                return _createSuperUserViewModel;

            return _loginScreenViewModel;
        }

        [ObservableProperty] private string? _applicationTitle = ApplicationName;
        [ObservableProperty] private ObservableObject _currentView;

        private const string ApplicationName = "Libota Desktop";
    }
}