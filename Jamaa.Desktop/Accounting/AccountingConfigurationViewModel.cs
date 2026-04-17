using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class AccountingConfigurationViewModel : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("f6a7b8c9-d1e2-4f3a-b4c5-d6e7f8a9b0c1");
    public string Title => "Accounting Configuration";
    public object? HeaderContent => null;
}
