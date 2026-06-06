using System;
using Domain.Organisation.Values;
using Domain.Shared.Values;

namespace Domain.Organisation.Requests;

public sealed record MemberRegistrationRequest
{
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public Gender Gender { get; init; }
    public DateTime RegistrationBegin { get; init; }
    public MembershipType MembershipType { get; init; }
    public required OrganisationId OrganisationId { get; init; }
}