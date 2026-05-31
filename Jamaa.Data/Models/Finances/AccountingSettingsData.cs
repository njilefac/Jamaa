using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jamaa.Data.Models.Finances;

public class AccountingSettingsData
{
    [Key] public required string OrganisationId { get; set; }
    public required string BaseCurrency { get; set; }
    public required string DateFormat { get; set; }
    public int DecimalPrecision { get; set; }
    public required string ThousandSeparator { get; set; }

    public IList<AccountingAvailableCurrencyData> AvailableCurrencies { get; set; } =
        new List<AccountingAvailableCurrencyData>();
}
