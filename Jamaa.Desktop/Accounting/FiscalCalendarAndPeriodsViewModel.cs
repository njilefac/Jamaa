using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class FiscalCalendarAndPeriodsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    public Guid Id => Guid.Parse("76a6a087-42cf-4495-8d6f-48ec84f917da");
    public string Title => "Fiscal Calendar & Periods";
    public object? HeaderContent => null;
}