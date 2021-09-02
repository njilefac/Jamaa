using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Setup
{
    public class CreateOrganizationViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "";
        
        public IScreen HostScreen { get; }
    }
}