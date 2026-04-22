using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record DeleteFiscalYear(OrganisationId OrganisationId, FiscalYearId FiscalYearId);

