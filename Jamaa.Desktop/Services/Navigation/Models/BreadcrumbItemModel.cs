using System.Windows.Input;

namespace Jamaa.Desktop.Services.Navigation.Models;

public record BreadcrumbItemModel(
    string Title,
    string TargetRoute,
    bool IsActive = false,
    ICommand? ClickCommand = null)
{
    public bool IsReadOnly => !IsActive;
}