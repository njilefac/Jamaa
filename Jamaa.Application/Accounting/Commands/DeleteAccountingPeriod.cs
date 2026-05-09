using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Accounting.Commands;

public record DeleteAccountingPeriod(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId);