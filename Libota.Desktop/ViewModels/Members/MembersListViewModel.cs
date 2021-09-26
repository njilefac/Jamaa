using System;
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
        public MembersListViewModel(
            IOrganisationManagementFacade organisationManagementFacade)
        {
            var membersSourceList = new SourceCache<Member, string>(m => m.Id);
            membersSourceList.PopulateFrom(organisationManagementFacade.MemberAdded);
            membersSourceList.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(Members)
                .DisposeMany()
                .Subscribe();
        }

        [Reactive] public ObservableCollectionExtended<Member> Members { get; set; } = new();
    }
}