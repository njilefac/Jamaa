using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Events
{
    public class EventsOverviewPageViewModel(IUserSessionService userSessionService) : ObservableObject;
}