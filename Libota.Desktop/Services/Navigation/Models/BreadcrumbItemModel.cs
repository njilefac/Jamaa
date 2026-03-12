using System.Windows.Input;

namespace Libota.Desktop.Services.Navigation.Models;

public record BreadcrumbItemModel(
    string Title,
    string TargetRoute,
    bool IsActive = false,
    ICommand? ClickCommand = null);