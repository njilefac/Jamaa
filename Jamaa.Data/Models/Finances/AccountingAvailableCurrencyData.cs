namespace Jamaa.Data.Models.Finances;

public class AccountingAvailableCurrencyData
{
    public required string OrganisationId { get; set; }
    public required string CurrencyCode { get; set; }
    public required string CurrencySymbol { get; set; }

    public AccountingSettingsData? AccountingSettings { get; set; }
}
