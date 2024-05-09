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

    public string? Description { get; }

    public string Name { get; private set; } = string.Empty;
    [Key] public string? Id { get; set; }

    public IList<MemberData> Members { get; set; }
}