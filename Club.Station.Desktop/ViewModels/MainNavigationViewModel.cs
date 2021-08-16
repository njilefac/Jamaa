using System;
using System.Reactive;
using System.Reactive.Linq;
using Domain.Services;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.ViewModels
{
    public class MainNavigationViewModel : ReactiveObject
    {
        private readonly IUserSessionService _userSessionService;
        public IScreen HostScreen { get; }
        public ReactiveCommand<Unit, Unit> GoToUserManagement { get; }
        public ReactiveCommand<Unit, Unit> GoToMemberManagement { get; }
        public ReactiveCommand<Unit, Unit> GoToEventManagement { get; }
        public ReactiveCommand<Unit, Unit> GoToGroupManagement { get; }
        public ReactiveCommand<Unit, Unit> GoToFinancesManagement { get; }
        
        public MainNavigationViewModel(IScreen screen, IUserSessionService userSessionService)
        {
            _userSessionService = userSessionService;
            var userIsAuthenticated = _userSessionService.CurrentSession.Select(s => s is {Authenticated: true});
            HostScreen = screen;
            
            GoToUserManagement = ReactiveCommand.Create(() =>
            {
                if (Locator.Current.GetService<UserManagementViewModel>() != null)
                    HostScreen.Router.Navigate.Execute(Locator.Current.GetService<UserManagementViewModel>() 
                                                       ?? throw new InvalidOperationException());
            }, userIsAuthenticated);

            
            GoToMemberManagement = ReactiveCommand.Create(() =>
            {
                if (Locator.Current.GetService<MemberManagementViewModel>() != null)
                    HostScreen.Router.Navigate.Execute(Locator.Current.GetService<MemberManagementViewModel>() 
                                                       ?? throw new InvalidOperationException());
            }, userIsAuthenticated);
            
            GoToEventManagement = ReactiveCommand.Create(() =>
            {
                HostScreen.Router.Navigate.Execute(Locator.Current.GetService<EventManagementViewModel>() 
                                                   ?? throw new InvalidOperationException());
            }, userIsAuthenticated);
            
            GoToGroupManagement = ReactiveCommand.Create(() =>
            {
                HostScreen.Router.Navigate.Execute(Locator.Current.GetService<GroupManagementViewModel>() 
                                                   ?? throw new InvalidOperationException());
            }, userIsAuthenticated);
            
            GoToFinancesManagement = ReactiveCommand.Create(() =>
            {
                HostScreen.Router.Navigate.Execute(Locator.Current.GetService<FinanceManagementViewModel>() 
                                                   ?? throw new InvalidOperationException());
            }, userIsAuthenticated);
        }
    }
}