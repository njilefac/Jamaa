using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record CreateAccountingPeriod(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

