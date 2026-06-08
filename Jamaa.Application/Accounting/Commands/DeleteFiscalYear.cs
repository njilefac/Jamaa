using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Accounting.Commands;

public record DeleteFiscalYear(OrganisationId OrganisationId, FiscalYearId FiscalYearId);