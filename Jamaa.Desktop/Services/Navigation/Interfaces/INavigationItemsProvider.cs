using System.Collections.Generic;
using Jamaa.Desktop.Services.Navigation.Models;

namespace Jamaa.Desktop.Services.Navigation.Interfaces;

public interface INavigationItemsProvider
{
    IEnumerable<NavigationItemModel> GetNavigationItems();
}
