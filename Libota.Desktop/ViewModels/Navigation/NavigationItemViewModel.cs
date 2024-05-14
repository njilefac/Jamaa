using System;
using System.Collections.Generic;

namespace Libota.Desktop.ViewModels.Navigation;

public record NavigationItemViewModel(string Title, 
    string Icon, 
    Type ViewModelType,
    bool Enabled = true, 
    IList<NavigationItemViewModel>? SubItems = null);