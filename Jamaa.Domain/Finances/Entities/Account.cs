using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Domain.Finances.Entities;

public class Account
{
    public AccountId Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public OrganisationId OrganisationId { get; set; }
    public AccountType Type { get; set; }
}