using Akka.Persistence;

namespace Jamaa.Application.Finances.Aggregates;

public class AccountAggregate : ReceivePersistentActor
{
    public override string PersistenceId { get; }
}