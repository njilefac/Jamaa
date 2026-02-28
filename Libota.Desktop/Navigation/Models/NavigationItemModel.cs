using System.Collections.Generic;

namespace Libota.Desktop.Navigation.Models;

public record NavigationItemModel(
    string TargetRoute,
    string Title,
    string Icon,
    bool Enabled = true,
    IList<NavigationItemModel>? SubItems = null);