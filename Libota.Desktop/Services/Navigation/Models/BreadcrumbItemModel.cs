namespace Libota.Desktop.Services.Navigation.Models;

public record BreadcrumbItemModel(
    string Title,
    string TargetRoute,
    bool IsActive = false);