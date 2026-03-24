using Domain.Organisation.Values;
using Domain.Shared.Values;

namespace Jamaa.Application.Organisation.Commands
{
    public record RegisterMember(
        OrganisationId OrganisationId,
        string FirstName,
        string? MiddleName,
        string LastName,
        Gender Gender,
        MembershipType MembershipType,
        DateTime RegistrationBegin);
}