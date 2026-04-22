using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Finances.Queries;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Queries.Finances;

public class FiscalCalendarQueryHandler(JamaaDbContext dbContext) : IFiscalCalendarQueryHandler
{
    public async Task<IList<FiscalYearData>> Get(GetFiscalYearsByOrganisation query)
    {
        var fiscalYears = await dbContext.FiscalYears
            .AsNoTracking()
            .Include(fiscalYear => fiscalYear.Periods)
            .Where(fiscalYear => fiscalYear.OrganisationId == query.OrganisationId.Value)
            .OrderByDescending(fiscalYear => fiscalYear.StartDate)
            .ToListAsync();

        foreach (var fiscalYear in fiscalYears)
        {
            fiscalYear.Periods = fiscalYear.Periods
                .OrderBy(period => period.SequenceNumber)
                .ThenBy(period => period.StartDate)
                .ToList();
        }

        return fiscalYears;
    }
}



