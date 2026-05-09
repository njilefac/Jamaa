using System.Collections.Generic;
using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Domain.Accounting.Entities;

public class ChartOfAccounts(ChartOfAccountsId id, OrganisationId organisationId)
{
    public ChartOfAccountsId Id { get; } = id;
    public OrganisationId OrganisationId { get; } = organisationId;
    public ICollection<Account> Accounts { get; } = [];
}