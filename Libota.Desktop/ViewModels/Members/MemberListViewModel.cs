using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Libota.Application.Organisation;
using Libota.Data.Models.Members;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MemberListViewModel : ObservableValidator
{
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    private readonly MemberProfileViewModel _memberProfileViewModel;
    private readonly INavigationService _navigationService;

    public MemberListViewModel(IOrganisationManagementFacade organisationManagementFacade,
        MemberProfileViewModel memberProfileViewModel, INavigationService navigationService)
    {
        _organisationManagementFacade = organisationManagementFacade;
        _memberProfileViewModel = memberProfileViewModel;
        _navigationService = navigationService;

        var membersSourceList = new SourceCache<MemberData, string>(m => m.Id);
            
        var members = new ObservableCollectionExtended<MemberData>();
        membersSourceList.PopulateFrom(organisationManagementFacade.CurrentMembers);
        membersSourceList.Connect()
            .Sort(SortExpressionComparer<MemberData>.Ascending(m => m.LastName))
            //.ObserveOn(RxApp.MainThreadScheduler)
            .Bind(Members)
            .DisposeMany()
            .Subscribe(changeSet =>
            {
                foreach (var change in changeSet)
                {
                    members.Add(change.Current);
                }
            });

        this.WhenValueChanged(x => x.SearchTerm, false)
            .Throttle(TimeSpan.FromSeconds(1))
            .Where(x => !string.IsNullOrWhiteSpace(x) || !string.IsNullOrWhiteSpace(SearchTerm))
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
    
    [ObservableProperty] private string? _searchTerm;
        
    [ObservableProperty] private ObservableCollectionExtended<MemberData> _members = new();
    public string UrlPathSegment => "members.list";

    [RelayCommand]
    private async Task<Unit> ShowMemberProfile(MemberData member)
    {
        _memberProfileViewModel.FirstName = member.FirstName;
        _memberProfileViewModel.MiddleName = member.MiddleName;
        _memberProfileViewModel.LastName = member.LastName;
        _memberProfileViewModel.Gender = member.Gender;
        _memberProfileViewModel.Registration = member.Registration;

        await _navigationService.NavigateTo(_memberProfileViewModel);

        return await Task.FromResult(Unit.Default);
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
}