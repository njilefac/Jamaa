using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Security;

namespace Libota.Desktop.ViewModels.Setup;

[UsedImplicitly]
public partial class CreateOrganisationViewModel(ISetupService setupService,
    CreateSuperUserViewModel createSuperUserViewModel,
    LoginScreenViewModel loginScreenViewModel,
    INavigationService navigationService) : ObservableValidator
{

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private string _city = string.Empty;

    [ObservableProperty] private string _street = string.Empty;

    [ObservableProperty] private string _houseNumber = string.Empty;

    [ObservableProperty] private string _postalCode = string.Empty;

    [ObservableProperty] private string _phoneNumber = string.Empty;

    [ObservableProperty] private string _website = string.Empty;

    public string UrlPathSegment => "setup.organization.create";

    [RelayCommand]
    private async Task CreateOrganisation()
    {
        var organisationWasCreated = await setupService.CreateOrganisation(Name.Trim(), Description.Trim());
        if (!organisationWasCreated) {};

        var superUser = await setupService.GetSuperUser();
        ObservableObject?  nextViewModel = superUser == null ? createSuperUserViewModel : loginScreenViewModel;
        navigationService.NavigateTo(nextViewModel ?? throw new InvalidOperationException());
    }
}