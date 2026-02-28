using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.Users;

public class AddUserScreenViewModel(IUserSessionService userSessionService): ObservableObject
{
    public string? UrlPathSegment => "add user";
}