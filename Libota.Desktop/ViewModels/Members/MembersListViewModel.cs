using System.Collections.Generic;
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
        private readonly IOrganisationManagementFacade _organisationManagementFacade;
        private readonly IUserSessionService _userSessionService;

        public MembersListViewModel(
            IOrganisationManagementFacade organisationManagementFacade,
            IUserSessionService userSessionService)
        {
            _organisationManagementFacade = organisationManagementFacade;
            _userSessionService = userSessionService;

            var currentOrganisation = _userSessionService.CurrentUserSession?.Organisation;
            Members = _organisationManagementFacade
                .ListMembersByOrganisation(new OrganisationId(currentOrganisation?.Id)).Result;
        }
        [Reactive] public IList<Member>? Members { get; set; }
    }
}