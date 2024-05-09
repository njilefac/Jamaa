using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Organisation.Requests;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;

namespace Libota.Application.Organisation
{
    public interface IOrganisationManagementFacade
    {
        Task CreateOrganisation(string name, string? description);
        Task RegisterMember(MemberRegistrationRequest request);
        Task<IEnumerable<OrganisationData>> ListOrganisations();
        IObservable<MemberData> MemberAdded { get; }
        IObservable<MemberData> MemberUpdated { get; }
        IObservable<MemberData> MemberDeleted { get; }
    }
}