using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Repositories.Finances;

// Operation: provides data access for accounting period entities
public interface IAccountingPeriodRepository
{
    Task<AccountingPeriodData?> GetByIdAsync(string periodId);
    Task<IList<AccountingPeriodData>> GetByFiscalYearAsync(string fiscalYearId);
    Task<bool> ExistsAsync(string periodId);
    Task AddAsync(AccountingPeriodData period);
    Task UpdateAsync(AccountingPeriodData period);
    Task DeleteAsync(string periodId);
}

public class AccountingPeriodRepository(JamaaDbContext dbContext) : IAccountingPeriodRepository
{
    // Operation: retrieves an accounting period by its id
    public async Task<AccountingPeriodData?> GetByIdAsync(string periodId)
    {
        return await dbContext.AccountingPeriods
            .FirstOrDefaultAsync(ap => ap.Id == periodId);
    }

    // Operation: retrieves all accounting periods for a fiscal year
    public async Task<IList<AccountingPeriodData>> GetByFiscalYearAsync(string fiscalYearId)
    {
        return await dbContext.AccountingPeriods
            .Where(ap => ap.FiscalYearId == fiscalYearId)
            .OrderBy(ap => ap.SequenceNumber)
            .ToListAsync();
    }

    // Operation: checks if an accounting period exists
    public async Task<bool> ExistsAsync(string periodId)
    {
        return await dbContext.AccountingPeriods.AnyAsync(ap => ap.Id == periodId);
    }

    // Operation: adds a new accounting period
    public async Task AddAsync(AccountingPeriodData period)
    {
        dbContext.AccountingPeriods.Add(period);
        await dbContext.SaveChangesAsync();
    }

    // Operation: updates an existing accounting period
    public async Task UpdateAsync(AccountingPeriodData period)
    {
        dbContext.AccountingPeriods.Update(period);
        await dbContext.SaveChangesAsync();
    }

    // Operation: deletes an accounting period
    public async Task DeleteAsync(string periodId)
    {
        var period = await dbContext.AccountingPeriods
            .FirstOrDefaultAsync(ap => ap.Id == periodId);

        if (period is not null)
        {
            dbContext.AccountingPeriods.Remove(period);
            await dbContext.SaveChangesAsync();
        }
    }
}



