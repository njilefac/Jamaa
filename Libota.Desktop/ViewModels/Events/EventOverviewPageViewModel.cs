using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Events
{
    public class EventOverviewPageViewModel(IUserSessionService userSessionService) : ObservableObject;
}