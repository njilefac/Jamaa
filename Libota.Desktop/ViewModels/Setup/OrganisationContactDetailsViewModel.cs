using System;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;
using Splat;

namespace Libota.Desktop.ViewModels.Setup
{
    public class OrganisationContactDetailsViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        public OrganisationContactDetailsViewModel()
        {
            HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();
        }

        public string UrlPathSegment => "setup.organisation.contact-details";
        public IScreen HostScreen { get; }
    }
}