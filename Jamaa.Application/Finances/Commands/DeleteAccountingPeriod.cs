using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record DeleteAccountingPeriod(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId);

