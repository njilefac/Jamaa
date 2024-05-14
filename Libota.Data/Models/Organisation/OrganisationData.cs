using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Libota.Data.Models.Members;

namespace Libota.Data.Models.Organisation;

public class OrganisationData
{
    public OrganisationData()
    {
        Members = new List<MemberData>();
    }

    public string? Description { get; set; }

    public string Name { get; set; }
    [Key] public string? Id { get; set; }

    public IList<MemberData> Members { get; set; }
}