using Avalonia.Threading;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Organisation.Values;
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
using Jamaa.Desktop.Members.ViewModels;
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
    public MemberEndRegistrationViewModel MemberEndRegistrationViewModel { get; }
    public Interaction<MemberRegistrationViewModel, DialogResponse<MemberRegistrationRequest>> AddMemberRegistration {get;} = new();
    public Interaction<MemberEndRegistrationViewModel, DialogResponse<RegistrationStatus>> ConfirmEndRegistration { get; } = new();
    public SelectionModel<MemberViewModel> Selection { get; }
    public string Title => "Overview";


    private readonly SynchronizationContext? _syncContext;

    public MemberListViewModel(IOrganisationManagementFacade organisationManagementFacade,
        MemberRegistrationViewModel memberRegistrationViewModel,
        MemberEndRegistrationViewModel memberEndRegistrationViewModel,
        Pages.MemberProfileViewModel memberProfileViewModel, 
        IRouteResolver routeResolver,
        INotificationService notificationService)
    {
        _syncContext = SynchronizationContext.Current;
        MemberRegistrationViewModel = memberRegistrationViewModel;
        MemberEndRegistrationViewModel = memberEndRegistrationViewModel;
        _organisationManagementFacade = organisationManagementFacade;
        _notificationService = notificationService;

        var membersSourceList = new SourceCache<MemberViewModel, string>(m => m.Id);
            
        var filter = this.WhenValueChanged(x => x.SearchTerm)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Select(BuildFilter);
        
        membersSourceList
            .Connect()
            .Filter(filter)
            .ObserveOn(_syncContext ?? SynchronizationContext.Current ?? new SynchronizationContext())
            .SortAndBind(Members, SortExpressionComparer<MemberViewModel>.Ascending(m => m.LastName))
            .DisposeMany()
            .Subscribe();

        _subscription = new CompositeDisposable(
            _organisationManagementFacade.CurrentMembers
                .Subscribe(m => membersSourceList.AddOrUpdate(MapToViewModel(m))),
            _organisationManagementFacade.MemberUpdated
                .Subscribe(m =>
                {
                    var existing = membersSourceList.Lookup(m.Id);
                    if (existing.HasValue)
                    {
                        existing.Value.UpdateFrom(m);
                    }
                    else
                    {
                        membersSourceList.AddOrUpdate(MapToViewModel(m));
                    }
                }),
            _organisationManagementFacade.MemberDeleted
                .Subscribe(m => membersSourceList.Remove(m.Id))
        );

        Selection = new() { Source = Members, SingleSelect = false };
    }

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
        if (parameter is MemberViewModel member)
        {
            // Note: MemberProfileNavigationArgs might still need MemberData 
            // but let's see if we can use MemberViewModel or map it back
            WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(new MemberProfileNavigationArgs(MapToData(member))));
        }
        else if (parameter is MemberProfileNavigationArgs args)
        {
            WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(args));
        }
    }

    private static bool MemberMatches(MemberViewModel member, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;
        return member.FirstName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               member.LastName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               !string.IsNullOrWhiteSpace(member.MiddleName) &&
               member.MiddleName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);
    }

    [RelayCommand]
    private async Task EndRegistration(MemberViewModel member)
    {
        if (member.Registration is { EndDate: not null })
        {
            return;
        }

        MemberEndRegistrationViewModel.Reset(member);
        var response = await ConfirmEndRegistration.Handle(MemberEndRegistrationViewModel);
        if (!response.Confirmed)
        {
            return;
        }

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

        await _organisationManagementFacade.UpdateMember(request);
        _notificationService.Show("Success", $"Registration for {member.FirstName} {member.LastName} has been ended.", NotificationType.Success);
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
            Registration = member.Registration == null ? null : new RegistrationViewModel
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
            Registration = member.Registration == null ? null : new RegistrationData
            {
                Id = member.Registration.Id,
                StartDate = member.Registration.StartDate,
                EndDate = member.Registration.EndDate,
                MembershipType = member.Registration.MembershipType,
                Status = member.Registration.Status
            }
        }!;
    }
    
    [ObservableProperty] private string? _searchTerm;
    [ObservableProperty] private object _activeContent;
    [ObservableProperty] private ObservableCollectionExtended<MemberViewModel> _members = [];
    [ObservableProperty] private MemberListDisplayMode _displayMode = MemberListDisplayMode.Card;
    private static bool CanShowMemberProfile(object? parameter) => true;
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    private readonly INotificationService _notificationService;
    private readonly IDisposable? _subscription;
}