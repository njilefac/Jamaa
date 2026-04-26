using Akka.Persistence.Journal;
using Jamaa.Application.Finances.Events;
using Jamaa.Application.Members.Events;
using Jamaa.Application.Organisation.Events;

namespace Jamaa.Application.Shared;

public sealed class LibotaEventTagger : IWriteEventAdapter
{
    public const string OrganisationEvent = "OrganisationEvent";
    public const string OrganisationCreated = "OrganisationCreated";
    public const string OrganisationChanged = "OrganisationChanged";
    public const string MemberRegistered = "MemberRegistered";
    public const string MemberChanged = "MemberChanged";
    public const string FinanceChanged = "FinanceChanged";
    
    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        return evt switch
        {
            OrganisationCreated organisationCreated => new Tagged(organisationCreated, new[] { OrganisationEvent, OrganisationCreated }),
            MemberRegistered memberRegistered => new Tagged(memberRegistered, new[] { OrganisationEvent, OrganisationChanged,  MemberRegistered }),
            MemberRegistrationUpdated memberRegistrationUpdated => new Tagged(memberRegistrationUpdated, new [] { OrganisationEvent, MemberChanged }), 
            MemberRegistrationEnded memberRegistrationEnded => new Tagged(memberRegistrationEnded, new [] { OrganisationEvent, MemberChanged }), 
            MemberUpdated memberUpdated => new Tagged(memberUpdated, new [] { OrganisationEvent, MemberChanged }),
            FiscalYearCreated fiscalYearCreated => new Tagged(fiscalYearCreated, new[] { OrganisationEvent, FinanceChanged }),
            FiscalYearUpdated fiscalYearUpdated => new Tagged(fiscalYearUpdated, new[] { OrganisationEvent, FinanceChanged }),
            FiscalYearDeleted fiscalYearDeleted => new Tagged(fiscalYearDeleted, new[] { OrganisationEvent, FinanceChanged }),
            AccountingPeriodCreated accountingPeriodCreated => new Tagged(accountingPeriodCreated, new[] { OrganisationEvent, FinanceChanged }),
            AccountingPeriodUpdated accountingPeriodUpdated => new Tagged(accountingPeriodUpdated, new[] { OrganisationEvent, FinanceChanged }),
            AccountingPeriodDeleted accountingPeriodDeleted => new Tagged(accountingPeriodDeleted, new[] { OrganisationEvent, FinanceChanged }),
            FiscalYearPeriodsRegenerated fiscalYearPeriodsRegenerated => new Tagged(fiscalYearPeriodsRegenerated, new[] { OrganisationEvent, FinanceChanged }),
            AccountingSettingsUpdated accountingSettingsUpdated => new Tagged(accountingSettingsUpdated, new[] { OrganisationEvent, FinanceChanged }),
            _ => evt
        };
    }
}