using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Application.Users.Services;

namespace Jamaa.Desktop.Users;

public class AddUserScreenViewModel() : ObservableObject
{
    public string? UrlPathSegment => "add user";
}