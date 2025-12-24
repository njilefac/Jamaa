using System.Collections.Generic;

namespace Libota.Desktop.ViewModels.Navigation;

public record NavigationItemViewModel(
    string TargetRoute,
    string Title,
    string Icon,
    bool Enabled = true,
    IList<NavigationItemViewModel>? SubItems = null);
