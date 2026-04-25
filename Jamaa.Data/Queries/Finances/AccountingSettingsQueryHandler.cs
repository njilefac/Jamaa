using System.Threading.Tasks;
using Domain.Finances.Queries;
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
            .FirstOrDefaultAsync(settings => settings.OrganisationId == query.OrganisationId.Value);
    }
}

