using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Application.Members.Aggregates;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Members.Events;

public record MemberUpdated(
    MemberId Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    Gender Gender,
    DateTime? BirthDate,
    DateTime RegistrationBegin,
    MembershipType MembershipType,
    RegistrationStatus Status,
    OrganisationId OrganisationId,
    byte[]? Avatar = null) : ILibotaEvent
{
    public string EntityId => OrganisationId.Value;
}
