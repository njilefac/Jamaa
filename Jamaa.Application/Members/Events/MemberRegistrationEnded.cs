using Jamaa.Application.Members.Aggregates;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Members.Events;

public record MemberRegistrationEnded(MemberId Id) : IJamaaEvent
{
    public string EntityId => Id.Value;
}