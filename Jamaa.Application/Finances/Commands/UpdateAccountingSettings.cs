using System.Collections.Generic;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Values;

namespace Jamaa.Application.Finances.Commands;

public record UpdateAccountingSettings(
    OrganisationId OrganisationId,
    string BaseCurrency,
    string DateFormat,
    int DecimalPrecision,
    List<Currency> AvailableCurrencies);
