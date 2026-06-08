using Domain.Accounting.Values;
using Jamaa.Application.Accounting.Models;

namespace Jamaa.Application.Accounting;

public interface IAccountingFacade
{
    // Reactive streams (push full fiscal-calendar snapshots)
    IObservable<FiscalCalendarData> CurrentFiscalCalendar { get; }
    IObservable<FiscalCalendarData> FiscalCalendarUpdated { get; }

    // Reactive streams (push chart-of-accounts row changes)
    IObservable<AccountData> AccountCreated { get; }
    IObservable<AccountData> AccountUpdated { get; }
    IObservable<AccountData> AccountDeleted { get; }
    IObservable<AccountData> AccountDeactivated { get; }
    IObservable<AccountData> AccountReactivated { get; }
    IObservable<AccountingPeriodBalanceData> AccountOpeningBalanceSet { get; }

    // Reactive streams (push accounting settings changes)
    IObservable<AccountingSettingsData?> CurrentAccountingSettings { get; }

    IObservable<AccountingSettingsData> AccountingSettingsUpdated { get; }

    // Write operations (fire-and-forget via commands)
    Task CreateFiscalYear(string organisationId, DateTime startDate, DateTime endDate, bool isLocked);

    Task UpdateFiscalYear(string organisationId, string fiscalYearId, DateTime startDate, DateTime endDate,
        bool isLocked);

    Task DeleteFiscalYear(string organisationId, string fiscalYearId);

    Task CreateAccount(string organisationId, string code, string name, string description, AccountType type,
        string? parentAccountId, bool isContraAccount = false);

    Task UpdateAccount(string organisationId, string accountId, string code, string name, string description,
        AccountType type, string? parentAccountId, bool isContraAccount = false);

    Task DeleteAccount(string organisationId, string accountId);
    Task DeactivateAccount(string organisationId, string accountId);
    Task ReactivateAccount(string organisationId, string accountId);
    Task SetAccountOpeningBalance(string organisationId, string accountId, string fiscalYearId,
        string accountingPeriodId, decimal openingBalance);

    Task CreateAccountingPeriod(string organisationId, string fiscalYearId, int sequenceNumber, DateTime startDate,
        DateTime endDate, bool isLocked);

    Task UpdateAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId,
        int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked);

    Task DeleteAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId);

    Task UpdateAccountingSettings(string organisationId, string baseCurrency, string dateFormat, int decimalPrecision,
        string thousandSeparator, IReadOnlyList<Currency> availableCurrencies);

    // Read operations (one-off query)
    Task<ChartOfAccountsData> GetChartOfAccounts(string organisationId);
    Task<FiscalCalendarData> GetFiscalCalendar(string organisationId);
    Task<AccountingSettingsData?> GetAccountingSettings(string organisationId);
    Task<decimal> GetAccountOpeningBalance(string organisationId, string accountId, string fiscalYearId,
        string accountingPeriodId);
    Task<bool> IsAccountingSetupComplete(string organisationId);
}
