using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record CreateFiscalYear(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

