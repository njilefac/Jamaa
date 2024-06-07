using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Libota.Application.Organisation;
using Libota.Data.Models.Members;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace Libota.Desktop.ViewModels.Members;

public class MembersListViewModel : ReactiveObject, IRoutableViewModel
{
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    [Reactive] public string? SearchTerm { get; set; }

    public ReactiveCommand<MemberData, Unit> ShowMemberDetails { get; set; }

    public MembersListViewModel()
    {
        HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();
        _organisationManagementFacade = Locator.Current.GetService<IOrganisationManagementFacade>() ?? throw new InvalidOperationException();

        var membersSourceList = new SourceCache<MemberData, string>(m => m.Id);
            
        var members = new ObservableCollectionExtended<MemberData>();
        membersSourceList.PopulateFrom(_organisationManagementFacade.CurrentMembers);
        membersSourceList.Connect()
            .Sort(SortExpressionComparer<MemberData>.Ascending(m => m.LastName))
            .ObserveOn(RxApp.MainThreadScheduler)
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
            .WhereNotNull()
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

        ShowMemberDetails = ReactiveCommand.CreateFromTask<MemberData, Unit>(ShowMemberProfile);
    }
        
    [Reactive] public ObservableCollectionExtended<MemberData> Members { get; set; } = new();
    public string? UrlPathSegment => "members.list";
    public IScreen HostScreen { get; }

    private async Task<Unit> ShowMemberProfile(MemberData member)
    {
        var memberProfileViewModel = Locator.Current.GetService<MemberProfileViewModel>();
        if (memberProfileViewModel == null) return await Task.FromResult(Unit.Default);

        memberProfileViewModel.FirstName = member.FirstName;
        memberProfileViewModel.MiddleName = member.MiddleName;
        memberProfileViewModel.LastName = member.LastName;
        memberProfileViewModel.Gender = member.Gender;
        memberProfileViewModel.Registration = member.Registration;

        await HostScreen.Router.Navigate.Execute(memberProfileViewModel);

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