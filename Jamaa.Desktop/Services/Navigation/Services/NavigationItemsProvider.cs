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
            new NavigationItemModel(Routes.AccountingOverview, "Accounting", "Icons.Accounting", SubItems:[
                new NavigationItemModel(Routes.AccountingDashboard, "Overview", "Icons.Accounting"),
                new NavigationItemModel(Routes.AccountingTransactions, "Journal Entries", "Icons.Accounting"),
                new NavigationItemModel(Routes.AccountingTransactions, "Bank Reconciliation", "Icons.Accounting"),
                new NavigationItemModel(Routes.ChartOfAccounts, "Chart of Accounts", "Icons.Accounting"),
                new NavigationItemModel(Routes.AccountingReports, "Reports", "Icons.Analytics"),
            ]),
        ];
    }
}










       
        
         
          
          
          
          