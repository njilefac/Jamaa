using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public class AutomationRulesViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    public Guid Id => Guid.Parse("29315df6-29e2-40f9-81ac-8b3431df7b1a");
    public string Title => "Automation Rules";
    public object? HeaderContent => null;
}