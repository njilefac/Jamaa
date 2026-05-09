using Domain.Organisation.Values;
using Jamaa.Application.Accounting.Values;

namespace Jamaa.Application.Accounting.Commands;

public record UpdateAccountingSettings(
    OrganisationId OrganisationId,
    string BaseCurrency,
    string DateFormat,
    int DecimalPrecision,
    List<Currency> AvailableCurrencies);