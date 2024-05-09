using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Setup
{
    public class OrganisationContactDetailsViewModel(IScreen hostScreen) : ReactiveValidationObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "setup.organisation.contact-details";
        public IScreen HostScreen { get; } = hostScreen;
    }
}