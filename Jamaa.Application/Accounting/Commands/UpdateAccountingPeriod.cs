using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Accounting.Commands;

public record UpdateAccountingPeriod(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);