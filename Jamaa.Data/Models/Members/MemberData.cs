using System.ComponentModel.DataAnnotations;
using Domain.Shared.Values;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Data.Models.Members;

public class MemberData
{
    [Key] public required string Id { get; set; }
    public required string LastName { get; set; }
    public string? MiddleName { get; set; }
    public required string FirstName { get; set; }
    public Gender Gender { get; set; }
    public required OrganisationData Organisation { get; set; }
    public required string OrganisationId { get; set; }
    public required RegistrationData Registration { get; set; }
    public byte[]? PictureData { get; set; }
}