using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Accounting.Events;

public record AccountingSettingsUpdated(
    OrganisationId OrganisationId,
    string BaseCurrency,
    string DateFormat,
    int DecimalPrecision,
    string ThousandSeparator,
    List<Currency> AvailableCurrencies) : IJamaaEvent
{
    public string EntityId => OrganisationId.Value;
}
