using Domain.Accounting.Values;

namespace Jamaa.Application.Accounting.Models;

public class AccountData
{
    public required string Id { get; set; }
    public required string OrganisationId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string? ParentId { get; set; }
    public bool IsActive { get; set; } = true;

    public AccountData? Parent { get; set; }
}

public class AccountingPeriodBalanceData
{
    public required string Id { get; set; }
    public required string AccountId { get; set; }
    public required string FiscalYearId { get; set; }
    public required string AccountingPeriodId { get; set; }
    public required string OrganisationId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    public AccountData? Account { get; set; }
    public FiscalYearData? FiscalYear { get; set; }
    public AccountingPeriodData? AccountingPeriod { get; set; }
}

public class ChartOfAccountsData
{
    public required string OrganisationId { get; set; }
    public IList<AccountData> Accounts { get; set; } = new List<AccountData>();
}

public class FiscalCalendarData
{
    public required string OrganisationId { get; set; }
    public IList<FiscalYearData> FiscalYears { get; set; } = new List<FiscalYearData>();
}

public class AccountingPeriodData
{
    public required string Id { get; set; }
    public required string FiscalYearId { get; set; }
    public required string OrganisationId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsLocked { get; set; }

    public FiscalYearData? FiscalYear { get; set; }
}

public class FiscalYearData
{
    public required string Id { get; set; }
    public required string OrganisationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsLocked { get; set; }

    public IList<AccountingPeriodData> Periods { get; set; } = new List<AccountingPeriodData>();
}

public class AccountingAvailableCurrencyData
{
    public required string OrganisationId { get; set; }
    public required string CurrencyCode { get; set; }
    public required string CurrencySymbol { get; set; }

    public AccountingSettingsData? AccountingSettings { get; set; }
}

public class AccountingSettingsData
{
    public required string OrganisationId { get; set; }
    public required string BaseCurrency { get; set; }
    public required string DateFormat { get; set; }
    public int DecimalPrecision { get; set; }

    public IList<AccountingAvailableCurrencyData> AvailableCurrencies { get; set; } =
        new List<AccountingAvailableCurrencyData>();
}