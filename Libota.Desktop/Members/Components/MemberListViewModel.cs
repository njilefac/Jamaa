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
using Libota.Application.Organisation;
using Libota.Data.Models.Members;
using Libota.Desktop.Members.Messages;
using Libota.Desktop.Services.Interactions;
using Libota.Desktop.Services.Navigation.Interfaces;

namespace Libota.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberListViewModel : ObservableValidator, IRouteableViewModel, IDisposable
{
    public MemberRegistrationViewModel MemberRegistrationViewModel { get; }
    public Interaction<MemberRegistrationViewModel, DialogResponse<MemberRegistrationRequest>> AddMemberRegistration {get;} = new();
    public string Title => "Overview";


    public MemberListViewModel(IOrganisationManagementFacade organisationManagementFacade,
        MemberRegistrationViewModel memberRegistrationViewModel,
        Pages.MemberProfileViewModel memberProfileViewModel, 
        IRouteResolver routeResolver)
    {
        MemberRegistrationViewModel = memberRegistrationViewModel;
        _organisationManagementFacade = organisationManagementFacade;

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
        var request = await AddMemberRegistration.Handle(MemberRegistrationViewModel);
        if (request.Confirmed)
        {
            await _organisationManagementFacade.RegisterMember(request.Result);
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowMemberProfile))]
    private void ShowMemberProfile(MemberData member)
    {
        WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(member));
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _subscription.Dispose();
    }
    
    [ObservableProperty] private string? _searchTerm;
    [ObservableProperty] private object _activeContent;
    [ObservableProperty] private ObservableCollectionExtended<MemberData> _members = [];
    private static bool CanShowMemberProfile(MemberData member) => true;
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    private readonly IDisposable _subscription;
}