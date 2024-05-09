using System;
using System.Reactive;
using System.Threading.Tasks;
using Libota.Application.Setup;
using Libota.Desktop.ViewModels.Security;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Splat;

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

            this.ValidationRule(x => x.Description, _ => !string.IsNullOrWhiteSpace(Description), "description error message");

            CreateOrganisation = ReactiveCommand.CreateFromTask(HandleCreateOrganisation, this.IsValid());

            CreateOrganisation.Subscribe(organisationWasCreated =>
            {
                if (!organisationWasCreated) return;

                var superUserViewModel = Locator.Current.GetService<CreateSuperUserViewModel>();
                var loginScreenViewModel = Locator.Current.GetService<LoginScreenViewModel>();
                var superUser = setupService.GetSuperUser().Result;
                IRoutableViewModel? nextViewModel = superUser == null ? superUserViewModel : loginScreenViewModel;
                HostScreen.Router.Navigate.Execute(nextViewModel ?? throw new InvalidOperationException());
            });

            CreateOrganisation.ThrownExceptions.Subscribe(ex => { Console.Error.WriteLine(ex); });
        }

        private async Task<bool> HandleCreateOrganisation() =>
            await _setupService.CreateOrganisation(Name.Trim(), Description.Trim());

        [Reactive] public string Name { get; set; } = string.Empty;

        [Reactive] public string Description { get; set; } = string.Empty;

        [Reactive] public string City { get; set; } = string.Empty;

        [Reactive] public string Street { get; set; } = string.Empty;

        [Reactive] public string HouseNumber { get; set; } = string.Empty;

        [Reactive] public string PostalCode { get; set; } = string.Empty;

        [Reactive] public string PhoneNumber { get; set; } = string.Empty;

        [Reactive] public string Website { get; set; } = string.Empty;

        public string? UrlPathSegment => "setup.organization.create";

        public ReactiveCommand<Unit, bool> CreateOrganisation { get; set; }

        public IScreen HostScreen { get; }

        private readonly ISetupService _setupService;
    }
}