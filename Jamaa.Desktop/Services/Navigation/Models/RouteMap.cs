using System;

namespace Jamaa.Desktop.Services.Navigation.Models;

public record RouteMap(
    string Path,
    Type ViewModel,
    bool IsDefault = false,
    RouteMap[]? Nested = null,
    string? DependsOn = null,
    Action<string>? Init = null);