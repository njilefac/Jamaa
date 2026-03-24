using Akka.Persistence.Journal;
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
            _ => evt
        };
    }
}