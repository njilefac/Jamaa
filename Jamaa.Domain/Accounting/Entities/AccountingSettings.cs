using System.Collections.Generic;
using System.Linq;
using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Domain.Accounting.Entities;

public sealed record AccountingSettings
{
    private readonly List<Currency> _availableCurrencies;

    public AccountingSettings(
        OrganisationId organisationId,
        string baseCurrency,
        string dateFormat,
        int decimalPrecision,
        IEnumerable<Currency>? availableCurrencies = null)
    {
        OrganisationId = organisationId;
        BaseCurrency = baseCurrency;
        DateFormat = dateFormat;
        DecimalPrecision = decimalPrecision;
        _availableCurrencies = availableCurrencies?.ToList() ?? [];
    }

    public OrganisationId OrganisationId { get; }
    public string BaseCurrency { get; }
    public string DateFormat { get; }
    public int DecimalPrecision { get; }
    public IReadOnlyList<Currency> AvailableCurrencies => _availableCurrencies.AsReadOnly();
}