using System;
using System.Collections.Generic;
using Libota.Desktop.Navigation.Interfaces;
using Libota.Desktop.Navigation.Models;

namespace Libota.Desktop.Navigation.Services;

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
        if (routeMap.Nested == null)
        {
            return;
        }
        foreach (var nested in routeMap.Nested)
        {
            RegisterInternal(nested);
        }
    }

    public RouteMap? Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        
        _routeRegistrations.TryGetValue(path, out var routeMap);
        return routeMap;
    }
}