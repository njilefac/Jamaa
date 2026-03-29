using System;
using Domain.Organisation.Values;
using Domain.Shared.Values;

namespace Domain.Organisation.Requests;

public class MemberUpdateRequest
{
    public string MemberId { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public Gender Gender { get; set; }
    public DateTime RegistrationBegin { get; set; }
    public DateTime? RegistrationEnd { get; set; }
    public MembershipType MembershipType { get; set; }
    public RegistrationStatus Status { get; set; }
    public OrganisationId OrganisationId { get; set; }
    public byte[]? Avatar { get; set; }
}
