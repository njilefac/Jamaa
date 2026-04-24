using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Accounting;

/// <summary>
/// Operation: Represents an actionable shortcut button on the GL Dashboard.
/// </summary>
public sealed class DashboardShortcutViewModel : ObservableObject
{
    public DashboardShortcutViewModel(string title, string icon, IRelayCommand? action = null)
    {
        Title = title;
        Icon = icon;
        Action = action ?? new RelayCommand(static () => { });
    }

    public string Title { get; }
    public string Icon { get; }
    public IRelayCommand Action { get; }
}
