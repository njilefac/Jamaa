using Akka.Persistence;

namespace Libota.Application.Finances.Aggregates;

public class AccountAggregate : ReceivePersistentActor
{
    public override string PersistenceId { get; }
}