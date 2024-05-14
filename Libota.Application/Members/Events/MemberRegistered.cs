using System;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Libota.Application.Members.Aggregates;
using Libota.Application.Shared;

namespace Libota.Application.Members.Events
{
    public record MemberRegistered(
        MemberId Id,
        string FirstName,
        string? MiddleName,
        string LastName,
        Gender Gender,
        DateTime? BirthDate,
        DateTime RegistrationBegin,
        MembershipType MembershipType) : ILibotaEvent

    {
        public string EntityId => Id.Value;
    }
}