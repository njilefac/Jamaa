using System;
using System.Collections.Generic;
using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Domain.Shared.Values;

namespace Domain.Accounting.Entities;

public class FiscalYear(
    FiscalYearId id,
    OrganisationId organisationId,
    DateTime startDate,
    DateTime endDate,
    bool isLocked = false) : ITimePeriod
{
    private readonly List<AccountingPeriod> _periods = [];

    public FiscalYearId Id { get; } = id;
    public OrganisationId OrganisationId { get; } = organisationId;
    public bool IsLocked { get; } = isLocked;
    public IReadOnlyList<AccountingPeriod> Periods => _periods.AsReadOnly();
    public DateTime Begin { get; } = startDate;
    public DateTime End { get; } = endDate;

    public void AddPeriod(AccountingPeriod period)
    {
        _periods.Add(period);
    }

    public override string ToString()
    {
        return $"FiscalYear {Begin:yyyy}-{End:yyyy}: {Begin:MM/dd/yyyy} to {End:MM/dd/yyyy}";
    }
}