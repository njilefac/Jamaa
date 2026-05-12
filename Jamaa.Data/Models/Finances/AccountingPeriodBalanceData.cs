using System.ComponentModel.DataAnnotations;

namespace Jamaa.Data.Models.Finances;

public class AccountingPeriodBalanceData
{
    [Key] public required string Id { get; set; }
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
