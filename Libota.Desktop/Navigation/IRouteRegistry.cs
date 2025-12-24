namespace Libota.Desktop.Navigation;

public interface IRouteRegistry
{
    void Register(RouteMap routeMap);
    RouteMap? Resolve(string path);
}