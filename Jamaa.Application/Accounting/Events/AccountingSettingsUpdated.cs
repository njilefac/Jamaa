using Domain.Organisation.Values;
using Jamaa.Application.Accounting.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Accounting.Events;

public record AccountingSettingsUpdated(
    OrganisationId OrganisationId,
    string BaseCurrency,
    string DateFormat,
    int DecimalPrecision,
    List<Currency> AvailableCurrencies) : IJamaaEvent
{
    public string EntityId => OrganisationId.Value;
}