using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersListViewModel : ReactiveObject, IRoutableViewModel
    {
        [Reactive] public string? SearchTerm { get; set; }
        
        public ReactiveCommand<Member, Unit> ShowMemberDetails { get; set; }
        public MembersListViewModel(
            IOrganisationManagementFacade organisationManagementFacade,
            IScreen hostScreen)
        {
            HostScreen = hostScreen;

            using (var membersSourceList = new SourceCache<Member, string>(m => m.Id))
            {
                var members = new ObservableCollectionExtended<Member>();
                membersSourceList.PopulateFrom(organisationManagementFacade.MemberAdded);
                membersSourceList.Connect()
                    .Sort(SortExpressionComparer<Member>.Ascending(m => m.LastName))
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
                            : members.Where(m => MemberMatches(m, term));
                    
                        membersSourceList.Edit(innerList =>
                        {
                            innerList.Clear();
                            innerList.AddOrUpdate(matchingMembers);
                        });
                    });
            }

            ShowMemberDetails = ReactiveCommand.CreateFromTask<Member, Unit>(ShowMemberProfile);
        }

        private async Task<Unit> ShowMemberProfile(Member member)
        {
            var memberProfileViewModel = Locator.Current.GetService<MemberProfileViewModel>();
            if (memberProfileViewModel == null) return await Task.FromResult(Unit.Default);
            
            memberProfileViewModel.FirstName = member.FirstName;
            memberProfileViewModel.MiddleName = member.MiddleName;
            memberProfileViewModel.LastName = member.LastName;
            memberProfileViewModel.Gender = member.Gender;
            memberProfileViewModel.Registration = member.Registration;
            
            HostScreen.Router.Navigate.Execute(memberProfileViewModel);
            
            return await Task.FromResult(Unit.Default);
        }

        private static bool MemberMatches(Member member, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;
            return member.FirstName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
                   member.LastName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ||
                   !string.IsNullOrWhiteSpace(member.MiddleName) &&
                   member.MiddleName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);
        }

        [Reactive] public ObservableCollectionExtended<Member> Members { get; set; } = new();
        public string? UrlPathSegment => "members.list";
        public IScreen HostScreen { get; }
    }
}