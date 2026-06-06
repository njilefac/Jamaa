using Domain.Organisation.Requests;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Organisation;

public interface IOrganisationFacade
{
    IObservable<MemberData> MemberUpdated { get; }
    IObservable<MemberData> MemberDeleted { get; }
    IObservable<MemberData> CurrentMembers { get; }
    Task CreateOrganisation(string name, string? description);
    Task RegisterMember(MemberRegistrationRequest request);
    Task UpdateMember(MemberUpdateRequest request);
    Task<IEnumerable<OrganisationData>> ListOrganisations();
}