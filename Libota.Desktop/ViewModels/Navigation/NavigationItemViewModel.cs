using System;
using System.Collections.Generic;

namespace Libota.Desktop.ViewModels.Navigation;

public record NavigationItemViewModel(
    Type ViewModelType,
    string Title,
    string Icon,
    bool Enabled = true,
    IList<NavigationItemViewModel>? SubItems = null);
