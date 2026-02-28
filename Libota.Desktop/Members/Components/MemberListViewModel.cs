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
using Libota.Desktop.Infrastructure.Interactions;
using Libota.Desktop.Members.Messages;
using Libota.Desktop.Navigation.Interfaces;
using Libota.Desktop.Navigation.Services;

namespace Libota.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberListViewModel : ObservableValidator, IRouteableViewModel
{
    public MemberRegistrationViewModel MemberRegistrationViewModel { get; }
    public Interaction<MemberRegistrationViewModel, DialogResponse<MemberRegistrationRequest>> AddMemberRegistration {get;} = new();   
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    private readonly IRouteResolver _routeResolver;


    public MemberListViewModel(
        IOrganisationManagementFacade organisationManagementFacade,
        MemberRegistrationViewModel memberRegistrationViewModel,
        Pages.MemberProfileViewModel memberProfileViewModel, 
        IRouteResolver routeResolver)
    {
        MemberRegistrationViewModel = memberRegistrationViewModel;
        _organisationManagementFacade = organisationManagementFacade;
        _routeResolver = routeResolver;

        var membersSourceList = new SourceCache<MemberData, string>(m => m.Id);
            
        var members = new ObservableCollectionExtended<MemberData>();
        membersSourceList.PopulateFrom(_organisationManagementFacade.CurrentMembers);
        membersSourceList
            .Connect()
            .SortAndBind(Members, SortExpressionComparer<MemberData>.Ascending(m => m.LastName))
            .DisposeMany()
            .Subscribe(changeSet =>
            {
                foreach (var change in changeSet)
                {
                    members.Add(change.Current);
                }
            });

        this.WhenValueChanged<MemberListViewModel, string>(x => x.SearchTerm, false)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(term =>
            {
                var matchingMembers = string.IsNullOrWhiteSpace(term)
                    ? members
                    : members.Where(m => MemberMatches(m, term.Trim()));

                membersSourceList.Edit(innerList =>
                {
                    innerList.Clear();
                    innerList.AddOrUpdate(matchingMembers);
                });
            });
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
    
    [ObservableProperty] private string? _searchTerm;
    [ObservableProperty] private object _activeContent;
        
    [ObservableProperty] private ObservableCollectionExtended<MemberData> _members = [];

    [RelayCommand(CanExecute = nameof(CanShowMemberProfile))]
    private void ShowMemberProfile(MemberData member)
    {
        WeakReferenceMessenger.Default.Send(new MemberDetailsRequested(member));
    }

    private static bool CanShowMemberProfile(MemberData member) => true  ;

    private static bool MemberMatches(MemberData member, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;
        return member.FirstName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               member.LastName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
               !string.IsNullOrWhiteSpace(member.MiddleName) &&
               member.MiddleName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);
    }

    public string Title => "Overview";
}