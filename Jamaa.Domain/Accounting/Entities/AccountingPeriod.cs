using System;
using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Domain.Shared.Values;

namespace Domain.Accounting.Entities;

public sealed record AccountingPeriod(
    AccountingPeriodId Id,
    FiscalYearId FiscalYearId,
    OrganisationId OrganisationId,
    int SequenceNumber,
    DateTime Begin,
    DateTime End,
    bool IsLocked = false) : ITimePeriod
{
    public override string ToString()
    {
        return $"Period {SequenceNumber}: {Begin:MM/dd/yyyy} to {End:MM/dd/yyyy}";
    }
}