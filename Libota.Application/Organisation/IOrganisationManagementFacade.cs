using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Organisation.Requests;

namespace Libota.Application.Organisation
{
    public interface IOrganisationManagementFacade
    {
        Task<bool> CreateOrganisation(string name, string? description);
        Task RegisterMember(MemberRegistrationRequest request);
        Task<IList<OrganisationReadModel>> ListOrganisations();
        IObservable<Member> MemberAdded { get; }
        IObservable<Member> MemberUpdated { get; }
        IObservable<Member> MemberDeleted { get; }
    }
}