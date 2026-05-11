using System;
using System.ComponentModel.DataAnnotations;
using Domain.Organisation.Values;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Data.Models.Members;

public class RegistrationData
{
    [Key] public required string Id { get; set; }
    public required MemberData Member { get; set; }
    public required string MemberId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public MembershipType MembershipType { get; set; }
    public RegistrationStatus Status { get; set; }
    public required OrganisationData Organisation { get; set; }
}