using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Libota.Data.Models.Members;

namespace Libota.Data.Models.Organisation;

public class OrganisationData
{
    [Key] public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public IList<MemberProfile> Members { get; set; } = new List<MemberProfile>();
}