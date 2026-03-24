using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Application.Members.Aggregates;

namespace Jamaa.Application.Organisation.Commands
{
    public record UpdateMember(
        OrganisationId OrganisationId,
        MemberId MemberId,
        string FirstName,
        string? MiddleName,
        string LastName,
        Gender Gender,
        MembershipType MembershipType,
        RegistrationStatus Status,
        DateTime RegistrationBegin);
}
