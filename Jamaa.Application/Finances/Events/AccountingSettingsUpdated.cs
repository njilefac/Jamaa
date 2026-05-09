using Domain.Organisation.Values;
using Jamaa.Application.Finances.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Events;

public record AccountingSettingsUpdated(
    OrganisationId OrganisationId,
    string BaseCurrency,
    string DateFormat,
    int DecimalPrecision,
    List<Currency> AvailableCurrencies) : IJamaaEvent
{
    public string EntityId => OrganisationId.Value;
}
