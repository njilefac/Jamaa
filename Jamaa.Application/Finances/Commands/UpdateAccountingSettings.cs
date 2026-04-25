using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record UpdateAccountingSettings(
    OrganisationId OrganisationId,
    string BaseCurrency,
    string DateFormat,
    int DecimalPrecision);

