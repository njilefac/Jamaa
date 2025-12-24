using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Setup;

public class OrganisationContactDetailsViewModel : ObservableObject
{
    public string RoutePath => Routes.OrganisationContactDetails;
    public void InitializeNavigation(INavigationScope navigationScope)
    {
        throw new System.NotImplementedException();
    }
}