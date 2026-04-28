using Akka.Actor;
using Akka.Persistence;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Finances.Events;

namespace Jamaa.Application.Finances.Aggregates;

public class AccountAggregate : ReceivePersistentActor
{
    public AccountAggregate(OrganisationId organisationId)
    {
        PersistenceId = $"account-{organisationId.Value}";

        Command<CreateAccount>(Handle);
        Command<UpdateAccount>(Handle);
        Command<DeleteAccount>(Handle);

        Recover<AccountCreated>(Apply);
        Recover<AccountUpdated>(Apply);
        Recover<AccountDeleted>(Apply);
    }

    public override string PersistenceId { get; }

    public static Props Props(OrganisationId organisationId)
    {
        return Akka.Actor.Props.Create(() => new AccountAggregate(organisationId));
    }

    private void Handle(CreateAccount command)
    {
        var @event = new AccountCreated(
            command.OrganisationId,
            command.AccountId,
            command.Code,
            command.Name,
            command.Type,
            command.ParentId);

        Persist(@event, Apply);
    }

    private void Handle(UpdateAccount command)
    {
        var @event = new AccountUpdated(
            command.OrganisationId,
            command.AccountId,
            command.Code,
            command.Name,
            command.Type,
            command.ParentId);

        Persist(@event, Apply);
    }

    private void Handle(DeleteAccount command)
    {
        var @event = new AccountDeleted(
            command.OrganisationId,
            command.AccountId);

        Persist(@event, Apply);
    }

    private void Apply(AccountCreated @event)
    {
        // State updates if needed
    }

    private void Apply(AccountUpdated @event)
    {
        // State updates if needed
    }

    private void Apply(AccountDeleted @event)
    {
        // State updates if needed
    }
}