using System.Collections.Generic;
using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Domain.Accounting.Entities;

/// <summary>
///     Represents the fiscal calendar for an organisation.
///     An organisation has exactly one fiscal calendar, which contains the full list of fiscal years.
/// </summary>
public class FiscalCalendar(FiscalCalendarId id, OrganisationId organisationId)
{
    private readonly List<FiscalYear> _fiscalYears = [];

    public FiscalCalendarId Id { get; } = id;
    public OrganisationId OrganisationId { get; } = organisationId;
    public IReadOnlyList<FiscalYear> FiscalYears => _fiscalYears.AsReadOnly();

    public void AddFiscalYear(FiscalYear fiscalYear)
    {
        _fiscalYears.Add(fiscalYear);
    }
}