using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Accounting.Commands;

public record CreateFiscalYear(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);