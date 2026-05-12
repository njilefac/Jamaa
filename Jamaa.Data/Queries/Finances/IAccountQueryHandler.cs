using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Accounting.Queries;
using Jamaa.Data.Models.Finances;

namespace Jamaa.Data.Queries.Finances;

public interface IAccountQueryHandler
{
    Task<IList<AccountData>> Get(GetAccountsByOrganisation query);
    Task<decimal> GetOpeningBalance(string organisationId, string accountId, string fiscalYearId,
        string accountingPeriodId);
}