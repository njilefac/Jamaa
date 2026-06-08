using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Repositories.Finances;

// Operation: provides data access for fiscal year entities
public interface IFiscalYearRepository
{
    Task<FiscalYearData?> GetByIdAsync(string fiscalYearId);
    Task<IList<FiscalYearData>> GetByOrganisationAsync(string organisationId);
    Task<bool> ExistsAsync(string fiscalYearId);
    Task AddAsync(FiscalYearData fiscalYear);
    Task UpdateAsync(FiscalYearData fiscalYear);
    Task DeleteAsync(string fiscalYearId);
}

public class FiscalYearRepository(JamaaDbContext dbContext) : IFiscalYearRepository
{
    // Operation: retrieves a fiscal year by its id, including related periods
    public async Task<FiscalYearData?> GetByIdAsync(string fiscalYearId)
    {
        return await dbContext.FiscalYears
            .Include(fy => fy.Periods)
            .FirstOrDefaultAsync(fy => fy.Id == fiscalYearId);
    }

    // Operation: retrieves all fiscal years for an organisation
    public async Task<IList<FiscalYearData>> GetByOrganisationAsync(string organisationId)
    {
        return await dbContext.FiscalYears
            .Include(fy => fy.Periods)
            .Where(fy => fy.OrganisationId == organisationId)
            .OrderByDescending(fy => fy.StartDate)
            .ToListAsync();
    }

    // Operation: checks if a fiscal year exists
    public async Task<bool> ExistsAsync(string fiscalYearId)
    {
        return await dbContext.FiscalYears.AnyAsync(fy => fy.Id == fiscalYearId);
    }

    // Operation: adds a new fiscal year
    public async Task AddAsync(FiscalYearData fiscalYear)
    {
        dbContext.FiscalYears.Add(fiscalYear);
        await dbContext.SaveChangesAsync();
    }

    // Operation: updates an existing fiscal year
    public async Task UpdateAsync(FiscalYearData fiscalYear)
    {
        dbContext.FiscalYears.Update(fiscalYear);
        await dbContext.SaveChangesAsync();
    }

    // Operation: deletes a fiscal year and its periods
    public async Task DeleteAsync(string fiscalYearId)
    {
        var fiscalYear = await dbContext.FiscalYears
            .Include(fy => fy.Periods)
            .FirstOrDefaultAsync(fy => fy.Id == fiscalYearId);

        if (fiscalYear is not null)
        {
            dbContext.FiscalYears.Remove(fiscalYear);
            await dbContext.SaveChangesAsync();
        }
    }
}