using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Accounting.Queries;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Queries.Finances;

public class AccountQueryHandler(JamaaDbContext dbContext) : IAccountQueryHandler
{
    public async Task<IList<AccountData>> Get(GetAccountsByOrganisation query)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.OrganisationId == query.OrganisationId.Value)
            .OrderBy(a => a.Code)
            .ToListAsync();
    }

    public async Task<decimal> GetOpeningBalance(string organisationId, string accountId, string fiscalYearId,
        string accountingPeriodId)
    {
        var id = $"{accountId}-{fiscalYearId}-{accountingPeriodId}";
        var balance = await dbContext.Set<AccountingPeriodBalanceData>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return balance?.OpeningBalance ?? 0m;
    }
}