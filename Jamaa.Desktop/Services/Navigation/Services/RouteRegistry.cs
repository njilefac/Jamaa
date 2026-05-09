using System;
using System.Collections.Generic;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;

namespace Jamaa.Desktop.Services.Navigation.Services;

public class RouteRegistry : IRouteRegistry
{
    private readonly Dictionary<string, RouteMap> _routeRegistrations = new();

    public void Register(RouteMap routeMap)
    {
        RegisterInternal(routeMap);
    }

    public RouteMap? Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        _routeRegistrations.TryGetValue(path, out var routeMap);
        return routeMap;
    }

    private void RegisterInternal(RouteMap routeMap)
    {
        _routeRegistrations.TryAdd(routeMap.Path, routeMap);
        if (routeMap.Nested == null) return;
        foreach (var nested in routeMap.Nested) RegisterInternal(nested);
    }
}