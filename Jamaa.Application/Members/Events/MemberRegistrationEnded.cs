using Jamaa.Application.Members.Aggregates;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Members.Events;

public record MemberRegistrationEnded(MemberId Id) : ILibotaEvent
{
    public string EntityId => Id.Value;
}