using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public class UserRolesAndApprovalsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    public Guid Id => Guid.Parse("ca4f6c71-8ab9-49f7-9734-dfa2b7ac8eb3");
    public string Title => "User Roles & Approvals";
    public object? HeaderContent => null;
}