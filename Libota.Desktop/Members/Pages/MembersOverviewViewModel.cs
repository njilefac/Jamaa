using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Libota.Application.Organisation;
using Libota.Desktop.Members.Components;
using Libota.Desktop.Members.Messages;
using Libota.Desktop.Services.Navigation.Interfaces;
using Libota.Desktop.Services.Navigation.Messages;
using Libota.Desktop.Services.Navigation.Models;
using Libota.Desktop.Services.Navigation.Values;
using Libota.Desktop.Shared;

namespace Libota.Desktop.Members.Pages;

[UsedImplicitly]
public partial class MembersOverviewViewModel : ObservableValidator,
    INavigationHost,
    IRecipient<NavigateBackRequested>,
    IRecipient<MemberDetailsRequested>,
    IDisposable,
    IApplicationModule
{
    private readonly IRouteResolver _routeResolver;
    private readonly LinkedList<IRouteableViewModel?> _navigationHistory = [];
    public ObservableCollection<BreadcrumbItemModel> Breadcrumbs { get; } = [];

    public MembersOverviewViewModel(
        IOrganisationManagementFacade organisationManagementFacade,
        MembersSummary membersSummary,
        IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        NavigateTo(Routes.MembersList);
        MembersSummary = membersSummary;

        WeakReferenceMessenger.Default.RegisterAll(this);

        
    }

    private void AddToNavigationHistory(IRouteableViewModel state)
    {
        _navigationHistory.AddLast(state);
        var breadCrumbClick = new RelayCommand(() => NavigateTo(state));
        Breadcrumbs.Add(new BreadcrumbItemModel(state.Title, string.Empty, ClickCommand: breadCrumbClick));
        Breadcrumbs[^1] = Breadcrumbs[^1] with { IsActive = false };
        // activate all except the last
        for (var i = 0; i < Breadcrumbs.Count - 1; i++)
        {
            Breadcrumbs[i] = Breadcrumbs[i] with { IsActive = true };
        }
    }

    private void NavigateTo(IRouteableViewModel state)
    {
        if (ActiveContent == state) return;

        ActiveContent = state;
        while (_navigationHistory.Last?.Value != state && _navigationHistory.Count > 1)
        {
            _navigationHistory.RemoveLast();
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        }
        
        Breadcrumbs[^1] = Breadcrumbs[^1] with { IsActive = false };
        for (var i = 0; i < Breadcrumbs.Count - 1; i++)
        {
            Breadcrumbs[i] = Breadcrumbs[i] with { IsActive = true };
        }
    }

    public void Receive(NavigateBackRequested message)
    {
        GoBack();
    }

    public void Receive(MemberDetailsRequested message)
    {
        var viewModel = _routeResolver.Resolve(Routes.MemberProfile, message.Member) as MemberProfileViewModel;
        if (viewModel != null)
        {
            viewModel.Initialize(message.Member);
            ActiveContent = viewModel;
            AddToNavigationHistory(ActiveContent);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
        MembersSummary.Dispose();
        
        foreach (var viewModel in _navigationHistory.OfType<IDisposable>())
        {
            viewModel.Dispose();
        }
    }

    public void NavigateTo<TViewModel>(object? parameter = null)
    {
        var matchingModel = _navigationHistory.OfType<TViewModel>().SingleOrDefault();
        if (matchingModel != null)
        {
            ActiveContent = matchingModel as IRouteableViewModel;
        }
    }

    public void NavigateTo(string route, object? parameter = null)
    {
        var matchingViewModel = _routeResolver.Resolve(route, parameter);
        if (_navigationHistory.Contains(matchingViewModel))
        {
            ActiveContent = _navigationHistory.SingleOrDefault(x => x == matchingViewModel);
            return;
        }

        ActiveContent = (IRouteableViewModel?)matchingViewModel;
        AddToNavigationHistory(ActiveContent ?? throw new InvalidOperationException());
    }

    public bool CanGoBack()
    {
        return _navigationHistory.ToList().IndexOf(ActiveContent) > 0;
    }

    public void GoBack()
    {
        if (!CanGoBack())
        {
            return;
        }

        var currentIndex = _navigationHistory.ToList().IndexOf(ActiveContent);
        ActiveContent = _navigationHistory.ToList()[currentIndex - 1];
        _navigationHistory.RemoveLast();
        Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
    }

    public bool CanGoForward()
    {
        var currentIndex = _navigationHistory.ToList().IndexOf(ActiveContent);
        return currentIndex < _navigationHistory.Count - 1;
    }

    public void GoForward()
    {
        if (!CanGoForward())
        {
            return;
        }

        var currentIndex = _navigationHistory.ToList().IndexOf(ActiveContent);
        ActiveContent = _navigationHistory.ToList()[currentIndex + 1];
    }

    public Guid Id => Guid.Parse("d1c8b9e7-5c3a-4f8e-9b2a-1f2e3d4c5b6a");
    public string Title  => "Members";
    public object HeaderContent => MembersSummary;
    
    [ObservableProperty] private IRouteableViewModel? _activeContent;
    [ObservableProperty] private MembersSummary _membersSummary;
}