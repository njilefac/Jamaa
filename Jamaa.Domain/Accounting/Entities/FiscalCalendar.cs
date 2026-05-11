using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Domain.Accounting.Entities;

/// <summary>
///     Represents the fiscal calendar for an organisation.
///     An organisation has exactly one fiscal calendar, which contains the full list of fiscal years.
/// </summary>
public class FiscalCalendar(FiscalCalendarId id, OrganisationId organisationId)
{
    public FiscalCalendarId Id { get; } = id;
    public OrganisationId OrganisationId { get; } = organisationId;
}