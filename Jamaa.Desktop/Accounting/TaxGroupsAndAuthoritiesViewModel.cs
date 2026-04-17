using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class TaxGroupsAndAuthoritiesViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    public Guid Id => Guid.Parse("2b778762-f5b4-4d46-b57f-dd89cc3f13e2");
    public string Title => "Tax Groups & Authorities";
    public object? HeaderContent => null;
}
