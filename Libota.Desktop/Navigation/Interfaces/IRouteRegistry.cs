using Libota.Desktop.Navigation.Models;

namespace Libota.Desktop.Navigation.Interfaces;

public interface IRouteRegistry
{
    void Register(RouteMap routeMap);
    RouteMap? Resolve(string path);
}