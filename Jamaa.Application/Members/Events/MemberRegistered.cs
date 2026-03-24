using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Application.Members.Aggregates;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Members.Events;

public record MemberRegistered(
    MemberId Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    Gender Gender,
    DateTime? BirthDate,
    DateTime RegistrationBegin,
    MembershipType MembershipType,
    OrganisationId OrganisationId) : ILibotaEvent

{
    public string EntityId => OrganisationId.Value;
}