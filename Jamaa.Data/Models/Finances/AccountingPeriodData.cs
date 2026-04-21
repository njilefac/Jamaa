using System;
using System.ComponentModel.DataAnnotations;

namespace Jamaa.Data.Models.Finances;

public class AccountingPeriodData
{
    [Key] public required string Id { get; set; }
    public required string FiscalYearId { get; set; }
    public required string OrganisationId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsLocked { get; set; }

    public FiscalYearData? FiscalYear { get; set; }
}


