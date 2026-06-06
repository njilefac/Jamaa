using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public class BankReconciliationViewModel : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("b2c3d4e5-f6a7-48b9-c0d1-e2f3a4b5c6d7");
    public string Title => "Bank Reconciliation";
    public object? HeaderContent => null;
}