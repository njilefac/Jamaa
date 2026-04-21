using Jamaa.Data.Models.Finances;

namespace Jamaa.Application.Finances;

public interface IFinanceManagementFacade
{
    // Write operations (fire-and-forget via commands)
    Task CreateFiscalYear(string organisationId, DateTime startDate, DateTime endDate, bool isLocked);
    Task UpdateFiscalYear(string organisationId, string fiscalYearId, DateTime startDate, DateTime endDate, bool isLocked);
    Task DeleteFiscalYear(string organisationId, string fiscalYearId);

    Task CreateAccountingPeriod(string organisationId, string fiscalYearId, int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked);
    Task UpdateAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId, int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked);
    Task DeleteAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId);

    // Read operations (one-off query)
    Task<IList<FiscalYearData>> GetFiscalYears(string organisationId);

    // Reactive streams (push new data when underlying data changes)
    IObservable<IList<FiscalYearData>> GetFiscalYearsStream(string organisationId);
}

