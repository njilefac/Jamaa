using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.Events
{
    public class EventsOverviewPageViewModel(IUserSessionService userSessionService) : ObservableObject;
}