using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jamaa.Data.Models.Finances;

public class FiscalYearData
{
    [Key] public required string Id { get; set; }
    public required string OrganisationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsLocked { get; set; }

    public IList<AccountingPeriodData> Periods { get; set; } = new List<AccountingPeriodData>();
}