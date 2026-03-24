using Jamaa.Desktop.Services.Navigation.Models;

namespace Jamaa.Desktop.Services.Navigation.Interfaces;

public interface IRouteRegistry
{
    void Register(RouteMap routeMap);
    RouteMap? Resolve(string path);
}