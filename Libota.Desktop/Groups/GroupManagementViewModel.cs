using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.Groups;

public class GroupManagementViewModel(IUserSessionService userSessionService): ObservableObject
{
    public string UrlPathSegment => "groups";
}