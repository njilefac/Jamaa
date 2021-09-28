using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersListViewModel : ReactiveObject
    {
        [Reactive] public string? SearchTerm { get; set; }

        public MembersListViewModel(
            IOrganisationManagementFacade organisationManagementFacade)
        {
            var membersSourceList = new SourceCache<Member, string>(m => m.Id);
            IObservableCollection<Member> members = new ObservableCollectionExtended<Member>();
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

        public IComparer<Member> CurrentComparer => new SortExpressionComparer<Member>();

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
    }
}