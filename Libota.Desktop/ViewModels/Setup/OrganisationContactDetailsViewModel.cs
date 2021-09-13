using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Setup
{
    public class OrganisationContactDetailsViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        public OrganisationContactDetailsViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
        }

        public string? UrlPathSegment => "setup.organisation.contact-details";
        public IScreen HostScreen { get; }
    }
}