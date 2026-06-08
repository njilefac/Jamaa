using Akka.Actor;
using Domain.Members;
using Jamaa.Application.Members.Commands;
using Jamaa.Application.Members.Events;

namespace Jamaa.Application.Members.Aggregates;

public class MemberAggregate : ReceiveActor
{
    public MemberAggregate(MemberId id)
    {
        _id = id;
        Receive<UpdateMemberRegistration>(OnUpdateRegistration);
    }

    private readonly MemberId _id;

    private void OnUpdateRegistration(UpdateMemberRegistration command)
    {
        throw new NotImplementedException();
    }

    public void Apply(MemberRegistered aggregateEvent)
    {
        throw new NotImplementedException();
    }

    public void Apply(MemberRegistrationUpdated aggregateEvent)
    {
        throw new NotImplementedException();
    }

    public void Apply(MemberContactDetailsChanged aggregateEvent)
    {
        throw new NotImplementedException();
    }
}