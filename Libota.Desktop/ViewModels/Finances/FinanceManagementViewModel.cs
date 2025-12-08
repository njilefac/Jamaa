using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Finances;

public class FinanceManagementViewModel(IUserSessionService userSessionService) : ObservableObject
{
    public string UrlPathSegment => "finance";
}