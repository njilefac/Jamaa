using System;

namespace Domain.Organisation.Values;

public record OrganisationId(string Value)
{
    public static OrganisationId With(Guid guid)
    {
        return new OrganisationId(guid.ToString());
    }
        
    public static OrganisationId With(string value)
    {
        return new OrganisationId(value);
    }
}