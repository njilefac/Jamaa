using System;
using System.Collections.Generic;

namespace Libota.Desktop.Navigation;

public class RouteRegistry : IRouteRegistry
{
    private readonly Dictionary<string, RouteMap> _routeRegistrations = new();
    public void Register(RouteMap routeMap)
    {
        RegisterInternal(routeMap);
    }

    private void RegisterInternal(RouteMap routeMap)
    {
        _routeRegistrations.TryAdd(routeMap.Path, routeMap);
        if (routeMap.Nested != null)
        {
            foreach (var nested in routeMap.Nested)
            {
                RegisterInternal(nested);
            }
        }
    }

    public RouteMap? Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        
        _routeRegistrations.TryGetValue(path, out var routeMap);
        return routeMap;
    }
}