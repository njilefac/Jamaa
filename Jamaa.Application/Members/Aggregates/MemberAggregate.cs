using Akka.Actor;
using Domain.Members;
using Jamaa.Application.Members.Commands;
using Jamaa.Application.Members.Events;

namespace Jamaa.Application.Members.Aggregates;

public class MemberAggregate : ReceiveActor
{
    private Member _state;

    public MemberAggregate(MemberId id)
    {
        Receive<UpdateMemberRegistration>(OnUpdateRegistration);
    }

    private void OnUpdateRegistration(UpdateMemberRegistration command)
    {
        throw new System.NotImplementedException();
    }

    public void Apply(MemberRegistered aggregateEvent)
    {
        throw new System.NotImplementedException();
    }

    public void Apply(MemberRegistrationUpdated aggregateEvent)
    {
        throw new System.NotImplementedException();
    }

    public void Apply(MemberContactDetailsChanged aggregateEvent)
    {
        throw new System.NotImplementedException();
    }
}