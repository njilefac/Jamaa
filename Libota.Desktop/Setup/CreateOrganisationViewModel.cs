using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Libota.Application.Setup;

namespace Libota.Desktop.Setup;

[UsedImplicitly]
public partial class CreateOrganisationViewModel(ISetupService setupService)
    : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "the name is required.")]
    [MinLength(3, ErrorMessage = "the name must be at least 3 characters.")]
    [NotifyCanExecuteChangedFor(nameof(CreateOrganisationCommand))]
    private string _name = string.Empty;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private string _city = string.Empty;

    [ObservableProperty] private string _street = string.Empty;

    [ObservableProperty] private string _houseNumber = string.Empty;

    [ObservableProperty] private string _postalCode = string.Empty;

    [ObservableProperty] private string _phoneNumber = string.Empty;

    [ObservableProperty] private string _website = string.Empty;

    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task CreateOrganisation()
    {
        var organisationWasCreated = await setupService.CreateOrganisation(Name.Trim(), Description.Trim());
        if (organisationWasCreated)
        {
            WeakReferenceMessenger.Default.Send(new OrganisationCreated());
        }
    }

    public bool IsValid
    {
        get
        {
            ValidateProperty(Name, nameof(Name));
            return !HasErrors;
        }
    }
}