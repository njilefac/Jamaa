using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Setup
{
    public class OrganisationContactDetailsViewModel(INavigationService navigationService) : ObservableObject
    {
        public string UrlPathSegment => "setup.organisation.contact-details";
    }
}