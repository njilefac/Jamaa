using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using DynamicData;
using DynamicData.Binding;
using Jamaa.Application.Organisation;
using Jamaa.Data.Models.Members;
using Jamaa.Desktop.Members.Messages;
using Jamaa.Desktop.Members.Pages;
using Jamaa.Desktop.Members.Values;
using Jamaa.Desktop.Members.ViewModels;
using Jamaa.Desktop.Services.Interactions;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Notifications;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberListViewModel : ObservableValidator, IRouteableViewModel, IDisposable
{
    private readonly INotificationService _notificationService;

    private readonly IOrganisationFacade _organisationFacade;
    private readonly IDisposable? _subscription;
    [ObservableProperty] private object? _activeContent;

    [ObservableProperty] private MemberListDisplayMode _displayMode = MemberListDisplayMode.List;

    [ObservableProperty] private bool _isOperationInFlight;

    private MemberViewModel? _lastSelectedMember;
    [ObservableProperty] private ObservableCollectionExtended<MemberViewModel> _members = [];

    [ObservableProperty] private string? _searchTerm;


    public MemberListViewModel(IOrganisationFacade organisationFacade,
        MemberRegistrationViewModel memberRegistrationViewModel,
        MemberEndRegistrationViewModel memberEndRegistrationViewModel,
        MemberProfileViewModel memberProfileViewModel,
        IRouteResolver routeResolver,
        INotificationService notificationService)
    {
        var syncContext = SynchronizationContext.Current;
        MemberRegistrationViewModel = memberRegistrationViewModel;
        MemberEndRegistrationViewModel = memberEndRegistrationViewModel;
        _organisationFacade = organisationFacade;
        _notificationService = notificationService;

        var membersSourceList = new SourceCache<MemberViewModel, string>(m => m.Id);

        var filter = this.WhenValueChanged(x => x.SearchTerm)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Select(BuildFilter);

        membersSourceList
            .Connect()
            .Filter(filter)
            .ObserveOn(syncContext ?? SynchronizationContext.Current ?? new SynchronizationContext())
            .SortAndBind(Members, SortExpressionComparer<MemberViewModel>.Ascending(m => m.LastName))
            .DisposeMany()
            .Subscribe(_ => EnsureSelection());

        _subscription = new CompositeDisposable(
            _organisationFacade.CurrentMembers
                .Subscribe(m => membersSourceList.AddOrUpdate(MapToViewModel(m))),
            _organisationFacade.MemberUpdated
                .Subscribe(m =>
                {
                    var existing = membersSourceList.Lookup(m.Id);
                    if (existing.HasValue)
                        existing.Value.UpdateFrom(m);
                    else
                        membersSourceList.AddOrUpdate(MapToViewModel(m));
                }),
            _organisationFacade.MemberDeleted
                .Subscribe(m => membersSourceList.Remove(m.Id))
        );

        Selection = new SelectionModel<MemberViewModel> { Source = Members, SingleSelect = true };
    }

    public MembershipType[] MembershipTypes => Enum.GetValues<MembershipType>();
    public RegistrationStatus[] RegistrationStatuses => Enum.GetValues<RegistrationStatus>();

    public MemberRegistrationViewModel MemberRegistrationViewModel { get; }
    public MemberEndRegistrationViewModel MemberEndRegistrationViewModel { get; }

    public Interaction<MemberRegistrationViewModel, DialogResponse<MemberRegistrationRequest>> AddMemberRegistration
    {
        get;
    } = new();

    public Interaction<MemberEndRegistrationViewModel, DialogResponse<RegistrationStatus>> ConfirmEndRegistration
    {
        get;
    } = new();

    public Interaction<Unit, Unit> FocusSearch { get; } = new();
    public SelectionModel<MemberViewModel> Selection { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _subscription?.Dispose();
    }

    public string Title => "Members";

    private static Func<MemberViewModel, bool> BuildFilter(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return _ => true;

        var search = term.Trim();
        return m => MemberMatches(m, search);
    }

    [RelayCommand]
    private async Task RegisterMember()
    {
        // INTEGRATION: Registers a member and waits for event confirmation
        if (IsOperationInFlight) return;

        MemberRegistrationViewModel.Reset();
        var request = await AddMemberRegistration.Handle(MemberRegistrationViewModel);
        if (request.Confirmed)
        {
            var subject = $"{request.Result.FirstName} {request.Result.LastName}";
            await _notificationService.TrackOperationAsync(
                () => _organisationFacade.RegisterMember(request.Result),
                _organisationFacade.CurrentMembers,
                m => m.FirstName == request.Result.FirstName && m.LastName == request.Result.LastName,
                TimeSpan.FromSeconds(10),
                "Member",
                "Registered",
                subject,
                SetOperationInFlight);
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowMemberProfile))]
    private void ShowMemberProfile(object? parameter)
    {
        if (parameter is MemberViewModel member)
            // Note: MemberProfileNavigationArgs might still need MemberData 
            // but let's see if we can use MemberViewModel or map it back
            WeakReferenceMessenger.Default.Send(
                new MemberDetailsRequested(new MemberProfileNavigationArgs(MapToData(member))));
        else if (parameter is MemberProfileNavigationArgs args)
            WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(args));
    }

    private static bool MemberMatches(MemberViewModel member, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;
        return member.FirstName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               member.LastName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               (!string.IsNullOrWhiteSpace(member.MiddleName) &&
                member.MiddleName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase));
    }

    [RelayCommand]
    private async Task EndRegistration(MemberViewModel member)
    {
        // INTEGRATION: Ends member registration and waits for event confirmation
        if (IsOperationInFlight || member.Registration is { EndDate: not null }) return;

        MemberEndRegistrationViewModel.Reset(member);
        var response = await ConfirmEndRegistration.Handle(MemberEndRegistrationViewModel);
        if (!response.Confirmed) return;

        var request = new MemberUpdateRequest
        {
            MemberId = member.Id,
            FirstName = member.FirstName,
            MiddleName = member.MiddleName,
            LastName = member.LastName,
            Gender = member.Gender,
            RegistrationBegin = member.Registration?.StartDate ?? DateTime.Now,
            RegistrationEnd = DateTime.Now,
            MembershipType = member.Registration?.MembershipType ?? default,
            Status = response.Result,
            OrganisationId = OrganisationId.With(member.OrganisationId),
            Avatar = member.PictureData
        };

        var subject = $"{member.FirstName} {member.LastName}";
        await _notificationService.TrackOperationAsync(
            () => _organisationFacade.UpdateMember(request),
            _organisationFacade.MemberUpdated,
            m => m.Id == member.Id,
            TimeSpan.FromSeconds(10),
            "Member",
            "Registration ended for",
            subject,
            SetOperationInFlight);
    }

    [RelayCommand]
    private async Task FocusSearchField()
    {
        await FocusSearch.Handle(Unit.Default);
    }

    [RelayCommand]
    private void UnselectAll()
    {
        Selection.Clear();
    }

    private void EnsureSelection()
    {
        if (Selection.SelectedItem == null && Members.Count > 0) Selection.SelectedIndex = 0;
    }

    private void SetOperationInFlight(bool isInFlight)
    {
        IsOperationInFlight = isInFlight;
    }

    private static MemberViewModel MapToViewModel(MemberData member)
    {
        return new MemberViewModel
        {
            Id = member.Id,
            FirstName = member.FirstName,
            MiddleName = member.MiddleName,
            LastName = member.LastName,
            Gender = member.Gender,
            OrganisationId = member.OrganisationId,
            PictureData = member.PictureData,
            Registration = new RegistrationViewModel
            {
                Id = member.Registration.Id,
                StartDate = member.Registration.StartDate,
                EndDate = member.Registration.EndDate,
                MembershipType = member.Registration.MembershipType,
                Status = member.Registration.Status
            }
        };
    }

    public static MemberData MapToData(MemberViewModel member)
    {
        return new MemberData
        {
            Id = member.Id,
            FirstName = member.FirstName,
            MiddleName = member.MiddleName,
            LastName = member.LastName,
            Gender = member.Gender,
            OrganisationId = member.OrganisationId,
            PictureData = member.PictureData,
            Registration = member.Registration == null
                ? new RegistrationData
                {
                    Id = Guid.NewGuid().ToString(),
                    MemberId = member.Id,
                    StartDate = DateTime.Now,
                    MembershipType = MembershipType.Regular,
                    Status = RegistrationStatus.Probation
                }
                : new RegistrationData
                {
                    Id = member.Registration.Id,
                    StartDate = member.Registration.StartDate,
                    EndDate = member.Registration.EndDate,
                    MembershipType = member.Registration.MembershipType,
                    Status = member.Registration.Status,
                    MemberId = member.Id
                }
        };
    }

    partial void OnDisplayModeChanging(MemberListDisplayMode value)
    {
        if (Selection.SelectedItem != null) _lastSelectedMember = Selection.SelectedItem;
    }

    partial void OnDisplayModeChanged(MemberListDisplayMode value)
    {
        // Use Post to ensure this runs after UI controls are detached/attached
        Dispatcher.UIThread.Post(() =>
        {
            if (_lastSelectedMember != null) Selection.SelectedItem = _lastSelectedMember;

            EnsureSelection();
        });
    }

    private static bool CanShowMemberProfile(object? parameter)
    {
        return true;
    }
}