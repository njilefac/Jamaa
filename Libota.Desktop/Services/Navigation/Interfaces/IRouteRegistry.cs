using Libota.Desktop.Services.Navigation.Models;

namespace Libota.Desktop.Services.Navigation.Interfaces;

public interface IRouteRegistry
{
    void Register(RouteMap routeMap);
    RouteMap? Resolve(string path);
}