using Akka.Actor;
using Akka.Persistence;
using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Accounting.Commands;
using Jamaa.Application.Accounting.Events;

namespace Jamaa.Application.Accounting.Aggregates;

public class AccountingPeriodBalanceAggregate : ReceivePersistentActor
{
    private readonly OrganisationId _organisationId;
    private readonly Dictionary<string, decimal> _openingBalances = new();

    public AccountingPeriodBalanceAggregate(OrganisationId organisationId)
    {
        _organisationId = organisationId;
        PersistenceId = $"accounting-period-balance-{organisationId.Value}";

        RegisterCommandHandlers();
        RegisterEventHandlers();
    }

    public static Props Props(OrganisationId organisationId) =>
        Akka.Actor.Props.Create(() => new AccountingPeriodBalanceAggregate(organisationId));

    private void RegisterCommandHandlers()
    {
        Command<SetAccountOpeningBalance>(Handle);
    }

    private void RegisterEventHandlers()
    {
        Recover<AccountOpeningBalanceSet>(Apply);
    }

    private void Handle(SetAccountOpeningBalance command)
    {
        // Composite key for state tracking if needed within actor, though we primarily care about persistence and projection
        var key = BuildKey(command.AccountId, command.FiscalYearId, command.AccountingPeriodId);
        
        Persist(new AccountOpeningBalanceSet(
            command.OrganisationId,
            command.AccountId,
            command.FiscalYearId,
            command.AccountingPeriodId,
            command.OpeningBalance), Apply);
    }

    private void Apply(AccountOpeningBalanceSet @event)
    {
        var key = BuildKey(@event.AccountId, @event.FiscalYearId, @event.AccountingPeriodId);
        _openingBalances[key] = @event.OpeningBalance;
    }

    private string BuildKey(AccountId accountId, FiscalYearId fiscalYearId, AccountingPeriodId periodId) =>
        $"{accountId.Value}|{fiscalYearId.Value}|{periodId.Value}";

    public override string PersistenceId { get; }
}
