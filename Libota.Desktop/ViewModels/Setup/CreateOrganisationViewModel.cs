using System;
using System.Reactive;
using System.Threading.Tasks;
using Libota.Application.Setup;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Setup
{
    public class CreateOrganisationViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        public CreateOrganisationViewModel(ISetupService setupService, IScreen hostScreen)
        {
            _setupService = setupService;
            HostScreen = hostScreen;

            this.ValidationRule(x => x.Name,
                v => !string.IsNullOrWhiteSpace(Name), "name validation error");
            
            this.ValidationRule(x => x.Description,
                v => !string.IsNullOrWhiteSpace(Description), "description error message");

            CreateOrganisation = ReactiveCommand.CreateFromTask(HandleCreateOrganisation, this.IsValid());
            CreateOrganisation.ThrownExceptions.Subscribe(ex =>
            {
                Console.Error.WriteLine(ex);
            });
        }

        private async Task<bool> HandleCreateOrganisation() => 
            await _setupService.CreateOrganisation(Name.Trim(), Description.Trim());

        [Reactive] public string Name { get; set; } = string.Empty;

        [Reactive] public string Description { get; set; }= string.Empty;

        [Reactive] public string City { get; set; }= string.Empty;

        [Reactive] public string Street { get; set; }= string.Empty;

        [Reactive] public string HouseNumber { get; set; }= string.Empty;

        [Reactive] public string PostalCode { get; set; }= string.Empty;

        [Reactive] public string PhoneNumber { get; set; }= string.Empty;

        [Reactive] public string Website { get; set; }= string.Empty;

        public string? UrlPathSegment => "setup.organization.create";

        public ReactiveCommand<Unit, bool> CreateOrganisation { get; set; }

        public IScreen HostScreen { get; }

        private readonly ISetupService _setupService;
    }
}