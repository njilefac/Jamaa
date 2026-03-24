using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Jamaa.Data.Models.Members;

namespace Jamaa.Data.Models.Organisation;

public class OrganisationData
{
    [Key] public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public IList<MemberData> Members { get; set; } = new List<MemberData>();
}