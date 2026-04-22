using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public class AccountingDashboardViewModel : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("d1c8e5b0-9f3a-4c8b-9e2a-1f2b3c4d5e6f");
    public string Title => "Accounting";
    public object? HeaderContent { get; } = null;
}