using System.Reactive;
using System.Threading.Tasks;
using Libota.Application.Setup;
using Libota.Desktop.Assets.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Setup
{
    public class CreateOrganizationViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        public CreateOrganizationViewModel(ISetupService setupService, IScreen hostScreen)
        {
            _setupService = setupService;
            HostScreen = hostScreen;

            this.ValidationRule(x => x.Name,
                v => !string.IsNullOrWhiteSpace(Name), string.Format(Messages.login_error_password));
            
            this.ValidationRule(x => x.Description,
                v => !string.IsNullOrWhiteSpace(Description), string.Format(Messages.login_error_password));

            CreateOrganization = ReactiveCommand.CreateFromTask(HandleCreateOrganization, this.IsValid());
        }

        private async Task<bool> HandleCreateOrganization()
        {
            return await _setupService.CreateOrganization(Name.Trim(), Description.Trim());
        }

        [Reactive] public string Name { get; set; } = string.Empty;

        [Reactive] public string Description { get; set; }= string.Empty;

        [Reactive] public string City { get; set; }= string.Empty;

        [Reactive] public string Street { get; set; }= string.Empty;

        [Reactive] public string HouseNumber { get; set; }= string.Empty;

        [Reactive] public string PostalCode { get; set; }= string.Empty;

        [Reactive] public string PhoneNumber { get; set; }= string.Empty;

        [Reactive] public string Website { get; set; }= string.Empty;

        public string? UrlPathSegment => "setup.organization.create";

        public ReactiveCommand<Unit, bool> CreateOrganization { get; set; }

        public IScreen HostScreen { get; }

        private readonly ISetupService _setupService;
    }
}