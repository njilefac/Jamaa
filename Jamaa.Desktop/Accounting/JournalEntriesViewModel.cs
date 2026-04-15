using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class JournalEntriesViewModel : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6");
    public string Title => "Journal Entries";
    public object? HeaderContent => null;
}
