namespace Libota.Desktop.Navigation;

public class NavigationService(IRouteResolver routeResolver) : INavigationService
{
    public INavigationScope CreateScope()
    {
        return new NavigationScope(routeResolver);
    }
}