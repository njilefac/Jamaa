using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record UpdateAccountingPeriod(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

