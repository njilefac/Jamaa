using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Organisation.Requests;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Avalonia.Controls.Selection;
using Jamaa.Application.Organisation;
using Jamaa.Data.Models.Members;
using Jamaa.Desktop.Members.Messages;
using Jamaa.Desktop.Members.Values;
using Jamaa.Desktop.Services.Interactions;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Notifications;

namespace Jamaa.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberListViewModel : ObservableValidator, IRouteableViewModel, IDisposable
{
    public MemberRegistrationViewModel MemberRegistrationViewModel { get; }
    public Interaction<MemberRegistrationViewModel, DialogResponse<MemberRegistrationRequest>> AddMemberRegistration {get;} = new();
    public SelectionModel<MemberData> Selection { get; }
    public MemberListDisplayMode[] DisplayModeOptions => Enum.GetValues<MemberListDisplayMode>();
    public string Title => "Overview";


    public MemberListViewModel(IOrganisationManagementFacade organisationManagementFacade,
        MemberRegistrationViewModel memberRegistrationViewModel,
        Pages.MemberProfileViewModel memberProfileViewModel, 
        IRouteResolver routeResolver,
        INotificationService notificationService)
    {
        MemberRegistrationViewModel = memberRegistrationViewModel;
        _organisationManagementFacade = organisationManagementFacade;
        _notificationService = notificationService;

        var membersSourceList = new SourceCache<MemberData, string>(m => m.Id);
            
        _subscription = membersSourceList.PopulateFrom(_organisationManagementFacade.CurrentMembers);

        var filter = this.WhenValueChanged(x => x.SearchTerm)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Select(BuildFilter);
        
        membersSourceList
            .Connect()
            .Filter(filter)
            .SortAndBind(Members, SortExpressionComparer<MemberData>.Ascending(m => m.LastName))
            .DisposeMany()
            .Subscribe();

        Selection = new() { Source = Members, SingleSelect = false };
    }

    private static Func<MemberData, bool> BuildFilter(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return _ => true;

        var search = term.Trim();
        return m => MemberMatches(m, search);
    }

    [RelayCommand]
    private async Task RegisterMember()
    {
        MemberRegistrationViewModel.Reset();
        var request = await AddMemberRegistration.Handle(MemberRegistrationViewModel);
        if (request.Confirmed)
        {
            await _organisationManagementFacade.RegisterMember(request.Result);
            _notificationService.Show("Success", $"Member {request.Result.FirstName} {request.Result.LastName} registered successfully.", NotificationType.Success);
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowMemberProfile))]
    private void ShowMemberProfile(object? parameter)
    {
        if (parameter is MemberData member)
        {
            WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(new MemberProfileNavigationArgs(member)));
        }
        else if (parameter is MemberProfileNavigationArgs args)
        {
            WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(args));
        }
    }

    private static bool MemberMatches(MemberData member, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;
        return member.FirstName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               member.LastName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               !string.IsNullOrWhiteSpace(member.MiddleName) &&
               member.MiddleName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);
    }

    [RelayCommand]
    private void SelectAll()
    {
        Selection.SelectAll();
    }

    [RelayCommand]
    private void UnselectAll()
    {
        Selection.Clear();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _subscription?.Dispose();
    }
    
    [ObservableProperty] private string? _searchTerm;
    [ObservableProperty] private object _activeContent;
    [ObservableProperty] private ObservableCollectionExtended<MemberData> _members = [];
    [ObservableProperty] private MemberListDisplayMode _displayMode = MemberListDisplayMode.Card;
    private static bool CanShowMemberProfile(object? parameter) => true;
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    private readonly INotificationService _notificationService;
    private readonly IDisposable? _subscription;
}