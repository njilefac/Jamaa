using Libota.Application.Members.Aggregates;
using Libota.Application.Shared;

namespace Libota.Application.Members.Events;

public record MemberRegistrationUpdated(MemberId Id) : ILibotaEvent
{
    public string EntityId => Id.Value;
}