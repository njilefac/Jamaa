using System.Threading.Tasks;
using Domain.Accounting.Queries;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Queries.Finances;

// Operation: retrieves the accounting settings read model for one organisation.
public class AccountingSettingsQueryHandler(JamaaDbContext dbContext) : IAccountingSettingsQueryHandler
{
    public Task<AccountingSettingsData?> Get(GetAccountingSettingsByOrganisation query)
    {
        return dbContext.AccountingSettings
            .AsNoTracking()
            .Include(settings => settings.AvailableCurrencies)
            .FirstOrDefaultAsync(settings => settings.OrganisationId == query.OrganisationId.Value);
    }
}