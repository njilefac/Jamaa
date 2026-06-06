using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public class AccountingReportsViewModel : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("c3d4e5f6-a7b8-49c9-d1e2-f3a4b5c6d7e8");
    public string Title => "Accounting Reports";
    public object? HeaderContent => null;
}