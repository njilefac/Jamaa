using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Groups;

public class GroupManagementViewModel(IUserSessionService userSessionService) : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
    public string Title => "Groups";
    public object? HeaderContent { get; } = null;
}