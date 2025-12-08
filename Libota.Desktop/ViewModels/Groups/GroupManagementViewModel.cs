using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Groups;

public class GroupManagementViewModel(IUserSessionService userSessionService): ObservableObject
{
    public string UrlPathSegment => "groups";
}