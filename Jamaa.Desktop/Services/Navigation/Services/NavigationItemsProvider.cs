using System.Collections.Generic;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using Jamaa.Desktop.Services.Navigation.Values;

namespace Jamaa.Desktop.Services.Navigation.Services;

public class NavigationItemsProvider : INavigationItemsProvider
{
    public IEnumerable<NavigationItemModel> GetNavigationItems()
    {
        //TODO: Load dynamically based on user permissions
        return
        [
            new NavigationItemModel(Routes.Dashboard, "Home", "Icons.Home"),
            new NavigationItemModel(Routes.MembersOverview, "Members", "Icons.Members"),
            new NavigationItemModel(Routes.EventsOverview, "Events", "Icons.Calendar"),
            new NavigationItemModel(Routes.AccountingDashboard, "Accounting", "Icons.Accounting"),
        ];
    }
}










       
        
         
          
          
          
          