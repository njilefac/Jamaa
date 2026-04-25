using System.Collections.Generic;
using Jamaa.Application.Finances.Values;
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

    Task UpdateAccountingSettings(string organisationId, string baseCurrency, string dateFormat, int decimalPrecision, IReadOnlyList<Currency> availableCurrencies);

    // Read operations (one-off query)
    Task<IList<FiscalYearData>> GetFiscalYears(string organisationId);
    Task<AccountingSettingsData?> GetAccountingSettings(string organisationId);

    // Reactive streams (push fiscal-year changes and current state)
    IObservable<FiscalYearData> CurrentFiscalYears { get; }
    IObservable<FiscalYearData> FiscalYearUpdated { get; }
    IObservable<FiscalYearData> FiscalYearDeleted { get; }

    // Reactive streams (push accounting settings changes)
    IObservable<AccountingSettingsData?> CurrentAccountingSettings { get; }
    IObservable<AccountingSettingsData> AccountingSettingsUpdated { get; }
}

