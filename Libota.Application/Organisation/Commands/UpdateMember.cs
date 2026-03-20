using Domain.Organisation.Values;
using Domain.Shared.Values;
using Libota.Application.Members.Aggregates;

namespace Libota.Application.Organisation.Commands
{
    public record UpdateMember(
        OrganisationId OrganisationId,
        MemberId MemberId,
        string FirstName,
        string? MiddleName,
        string LastName,
        Gender Gender,
        MembershipType MembershipType,
        DateTime RegistrationBegin);
}
