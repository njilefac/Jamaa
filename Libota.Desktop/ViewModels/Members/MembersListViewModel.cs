using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Users.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersListViewModel : ReactiveObject
    {
        public MembersListViewModel(
            IOrganisationManagementFacade organisationManagementFacade,
            IUserSessionService userSessionService)
        {
            var membersSourceList = new SourceCache<Member, string>(m => m.Id);

            var currentOrganisation = userSessionService.CurrentUserSession?.Organisation;

            Task.Run(async () =>
            {
                var members = await organisationManagementFacade
                    .ListMembersByOrganisation(new OrganisationId(currentOrganisation?.Id));
                if (members != null)
                    membersSourceList.AddOrUpdate(members);
            });

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