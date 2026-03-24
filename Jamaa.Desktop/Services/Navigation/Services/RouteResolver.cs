using System;
using Jamaa.Desktop.Services.Navigation.Interfaces;

namespace Jamaa.Desktop.Services.Navigation.Services;

public sealed class RouteResolver(IRouteRegistry routeRegistry, IServiceProvider serviceProvider) : IRouteResolver
{
    public object Resolve(string route, object? parameter)
    {
        var resolvedViewModel = routeRegistry.Resolve(route) is not { } routeMap
            ? throw new InvalidOperationException($"No route registered for path '{route}'")
            : serviceProvider.GetService(routeMap.ViewModel) ??
              throw new InvalidOperationException(
                  $"Could not resolve view model of type '{routeMap.ViewModel.FullName}' for route '{route}'");

        return resolvedViewModel;
    }
}