using Akka.Actor;
using Akka.Persistence;
using Domain.Finances.Queries;
using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Finances.Events;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Aggregates;

public class AccountAggregate : ReceivePersistentActor
{
    private readonly AccountStateStore _state = new();
    private readonly OrganisationId _organisationId;
    private readonly IQueryProcessor _queryProcessor;
    private bool _hasHydratedFromPersistence;

    public AccountAggregate(OrganisationId organisationId, IQueryProcessor queryProcessor)
    {
        _organisationId = organisationId;
        _queryProcessor = queryProcessor;
        PersistenceId = $"chart-of-accounts-{organisationId.Value}";

        RegisterCommandHandlers();
        RegisterEventHandlers();
    }

    public override string PersistenceId { get; }

    public static Props Props(OrganisationId organisationId, IQueryProcessor queryProcessor)
    {
        return new Props(typeof(AccountAggregate), [organisationId, queryProcessor]);
    }

    private void RegisterCommandHandlers()
    {
        Command<CreateAccount>(Handle);
        Command<UpdateAccount>(Handle);
        Command<DeleteAccount>(Handle);
        Command<DeactivateAccount>(Handle);
        Command<ReactivateAccount>(Handle);
    }

    private void RegisterEventHandlers()
    {
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is AccountStateStore state)
            {
                _state.CopyFrom(state);
            }
        });

        Recover<AccountCreated>(Apply);
        Recover<AccountUpdated>(Apply);
        Recover<AccountDeleted>(Apply);
        Recover<AccountDeactivated>(Apply);
        Recover<AccountReactivated>(Apply);
    }

    // Integration: validates and persists one new account in the organisation chart.
    private void Handle(CreateAccount command)
    {
        EnsureHydratedFromPersistence();

        if (!TryNormalizeAccount(command.Code, command.Name, command.Description, out var code, out var name, out var description))
        {
            return;
        }

        if (_state.Accounts.ContainsKey(command.AccountId.Value))
        {
            Sender.Tell("Account already exists.", Self);
            return;
        }

        if (HasDuplicateCode(code, command.AccountId.Value))
        {
            Sender.Tell("Account code already exists.", Self);
            return;
        }

        if (!TryValidateParentAccount(command.ParentId, command.Type, null))
        {
            return;
        }

        Persist(new AccountCreated(
            command.OrganisationId,
            command.AccountId,
            code,
            name,
            command.Type,
            command.ParentId,
            description), Apply);

        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: validates and persists updates to one existing account.
    private void Handle(UpdateAccount command)
    {
        EnsureHydratedFromPersistence();

        if (!_state.Accounts.ContainsKey(command.AccountId.Value))
        {
            Sender.Tell("Account not found.", Self);
            return;
        }

        if (!TryNormalizeAccount(command.Code, command.Name, command.Description, out var code, out var name, out var description))
        {
            return;
        }

        if (HasDuplicateCode(code, command.AccountId.Value))
        {
            Sender.Tell("Account code already exists.", Self);
            return;
        }

        if (!TryValidateParentAccount(command.ParentId, command.Type, command.AccountId.Value))
        {
            return;
        }

        Persist(new AccountUpdated(
            command.OrganisationId,
            command.AccountId,
            code,
            name,
            command.Type,
            command.ParentId,
            description), Apply);

        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: deletes one account after confirming no child accounts depend on it.
    private void Handle(DeleteAccount command)
    {
        EnsureHydratedFromPersistence();

        if (!_state.Accounts.ContainsKey(command.AccountId.Value))
        {
            Sender.Tell("Account not found.", Self);
            return;
        }

        if (_state.Accounts.Values.Any(account => account.ParentId == command.AccountId.Value))
        {
            Sender.Tell("Delete child accounts before deleting this account.", Self);
            return;
        }

        Persist(new AccountDeleted(command.OrganisationId, command.AccountId), Apply);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: marks one active account as inactive.
    private void Handle(DeactivateAccount command)
    {
        EnsureHydratedFromPersistence();

        if (!_state.Accounts.TryGetValue(command.AccountId.Value, out var account))
        {
            Sender.Tell("Account not found.", Self);
            return;
        }

        if (!account.IsActive)
        {
            Sender.Tell("Account is already inactive.", Self);
            return;
        }

        var deactivationEvents = BuildCascadeAccountIds(command.AccountId.Value, isActiveTarget: false)
            .Select(accountId => new AccountDeactivated(command.OrganisationId, AccountId.With(accountId)))
            .ToList();

        PersistAll(deactivationEvents, Apply);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: reactivates one inactive account.
    private void Handle(ReactivateAccount command)
    {
        EnsureHydratedFromPersistence();

        if (!_state.Accounts.TryGetValue(command.AccountId.Value, out var account))
        {
            Sender.Tell("Account not found.", Self);
            return;
        }

        if (account.IsActive)
        {
            Sender.Tell("Account is already active.", Self);
            return;
        }

        var reactivationEvents = BuildCascadeAccountIds(command.AccountId.Value, isActiveTarget: true)
            .Select(accountId => new AccountReactivated(command.OrganisationId, AccountId.With(accountId)))
            .ToList();

        PersistAll(reactivationEvents, Apply);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Operation: returns descendant account ids whose current active state differs from the requested target state.
    private IReadOnlyList<string> BuildCascadeAccountIds(string rootAccountId, bool isActiveTarget)
    {
        return AccountStateCascadePlanner.BuildCascadeAccountIds(GetAccountSnapshots(), rootAccountId, isActiveTarget);
    }

    // Operation: projects in-memory actor state into immutable snapshots for cascade planning.
    private IReadOnlyList<AccountStateSnapshot> GetAccountSnapshots()
    {
        return _state.Accounts.Values
            .Select(account => new AccountStateSnapshot(account.Id, account.Code, account.ParentId, account.IsActive))
            .ToList();
    }

    private void Apply(AccountCreated @event)
    {
        _state.Accounts[@event.AccountId.Value] = new AccountState
        {
            Id = @event.AccountId.Value,
            Code = @event.Code,
            Name = @event.Name,
            Description = @event.Description,
            Type = @event.Type,
            ParentId = @event.ParentId?.Value
        };
    }

    private void Apply(AccountUpdated @event)
    {
        if (!_state.Accounts.TryGetValue(@event.AccountId.Value, out var account))
        {
            return;
        }

        account.Code = @event.Code;
        account.Name = @event.Name;
        account.Description = @event.Description;
        account.Type = @event.Type;
        account.ParentId = @event.ParentId?.Value;
    }

    private void Apply(AccountDeleted @event)
    {
        _state.Accounts.Remove(@event.AccountId.Value);
    }

    private void Apply(AccountDeactivated @event)
    {
        if (_state.Accounts.TryGetValue(@event.AccountId.Value, out var account))
        {
            account.IsActive = false;
        }
    }

    private void Apply(AccountReactivated @event)
    {
        if (_state.Accounts.TryGetValue(@event.AccountId.Value, out var account))
        {
            account.IsActive = true;
        }
    }

    // Operation: validates and normalizes account fields.
    private bool TryNormalizeAccount(
        string requestedCode,
        string requestedName,
        string? requestedDescription,
        out string code,
        out string name,
        out string description)
    {
        code = requestedCode.Trim();
        name = requestedName.Trim();
        description = (requestedDescription ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            Sender.Tell("Account code is required.", Self);
            return false;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Sender.Tell("Account name is required.", Self);
            return false;
        }

        if (name.Length > 100)
        {
            Sender.Tell("Account name must be 100 characters or fewer.", Self);
            return false;
        }

        if (description.Length > 500)
        {
            Sender.Tell("Account description must be 500 characters or fewer.", Self);
            return false;
        }

        return true;
    }

    // Operation: determines whether another account already uses the requested code.
    private bool HasDuplicateCode(string code, string accountId)
    {
        return _state.Accounts.Values.Any(account => account.Id != accountId && account.Code == code);
    }

    // Operation: validates parent existence, type compatibility, and cycle safety.
    private bool TryValidateParentAccount(AccountId? parentAccountId, AccountType type, string? currentAccountId)
    {
        if (parentAccountId is null)
        {
            return true;
        }

        if (parentAccountId.Value == currentAccountId)
        {
            Sender.Tell("An account cannot be its own parent.", Self);
            return false;
        }

        if (!_state.Accounts.TryGetValue(parentAccountId.Value, out var parentAccount))
        {
            Sender.Tell("Parent account not found.", Self);
            return false;
        }

        if (parentAccount.Type != type)
        {
            Sender.Tell("Parent and child accounts must use the same account type.", Self);
            return false;
        }

        if (currentAccountId is null)
        {
            return true;
        }

        var cursor = parentAccount.ParentId;
        while (!string.IsNullOrWhiteSpace(cursor) && _state.Accounts.TryGetValue(cursor, out var ancestor))
        {
            if (ancestor.Id == currentAccountId)
            {
                Sender.Tell("The selected parent would create a circular account hierarchy.", Self);
                return false;
            }

            cursor = ancestor.ParentId;
        }

        return true;
    }

    // Operation: bootstraps in-memory state from persisted read-model rows when event-sourced recovery produced no accounts.
    private void EnsureHydratedFromPersistence()
    {
        if (_hasHydratedFromPersistence)
        {
            return;
        }

        if (_state.Accounts.Count > 0)
        {
            _hasHydratedFromPersistence = true;
            return;
        }

        var persistedAccounts = _queryProcessor
            .Get(new GetAccountsByOrganisation(_organisationId))
            .GetAwaiter()
            .GetResult();

        foreach (var persistedAccount in persistedAccounts)
        {
            if (_state.Accounts.ContainsKey(persistedAccount.Id))
            {
                continue;
            }

            _state.Accounts[persistedAccount.Id] = new AccountState
            {
                Id = persistedAccount.Id,
                Code = persistedAccount.Code,
                Name = persistedAccount.Name,
                Description = persistedAccount.Description,
                Type = persistedAccount.Type,
                ParentId = persistedAccount.ParentId,
                IsActive = persistedAccount.IsActive
            };
        }

        _hasHydratedFromPersistence = true;
    }

    private void TrySaveSnapshot()
    {
        if (LastSequenceNr % 20 == 0)
        {
            SaveSnapshot(_state.Clone());
        }
    }

    [Serializable]
    private sealed class AccountStateStore
    {
        public Dictionary<string, AccountState> Accounts { get; } = new();

        public AccountStateStore Clone()
        {
            var clone = new AccountStateStore();
            foreach (var account in Accounts)
            {
                clone.Accounts[account.Key] = account.Value.Clone();
            }

            return clone;
        }

        public void CopyFrom(AccountStateStore other)
        {
            Accounts.Clear();
            foreach (var account in other.Accounts)
            {
                Accounts[account.Key] = account.Value.Clone();
            }
        }
    }

    [Serializable]
    private sealed class AccountState
    {
        public string Id { get; init; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string? ParentId { get; set; }
        public bool IsActive { get; set; } = true;

        public AccountState Clone()
        {
            return new AccountState
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Type = Type,
                ParentId = ParentId,
                IsActive = IsActive
            };
        }
    }
}