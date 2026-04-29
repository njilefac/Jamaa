using Akka.Actor;
using Akka.Persistence;
using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Finances.Events;

namespace Jamaa.Application.Finances.Aggregates;

public class AccountAggregate : ReceivePersistentActor
{
    private readonly AccountStateStore _state = new();

    public AccountAggregate(OrganisationId organisationId)
    {
        PersistenceId = $"chart-of-accounts-{organisationId.Value}";

        RegisterCommandHandlers();
        RegisterEventHandlers();
    }

    public override string PersistenceId { get; }

    public static Props Props(OrganisationId organisationId)
    {
        return new Props(typeof(AccountAggregate), [organisationId]);
    }

    private void RegisterCommandHandlers()
    {
        Command<CreateAccount>(Handle);
        Command<UpdateAccount>(Handle);
        Command<DeleteAccount>(Handle);
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
    }

    // Integration: validates and persists one new account in the organisation chart.
    private void Handle(CreateAccount command)
    {
        if (!TryNormalizeAccount(command.Code, command.Name, out var code, out var name))
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
            command.ParentId), Apply);

        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: validates and persists updates to one existing account.
    private void Handle(UpdateAccount command)
    {
        if (!_state.Accounts.ContainsKey(command.AccountId.Value))
        {
            Sender.Tell("Account not found.", Self);
            return;
        }

        if (!TryNormalizeAccount(command.Code, command.Name, out var code, out var name))
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
            command.ParentId), Apply);

        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: deletes one account after confirming no child accounts depend on it.
    private void Handle(DeleteAccount command)
    {
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

    private void Apply(AccountCreated @event)
    {
        _state.Accounts[@event.AccountId.Value] = new AccountState
        {
            Id = @event.AccountId.Value,
            Code = @event.Code,
            Name = @event.Name,
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
        account.Type = @event.Type;
        account.ParentId = @event.ParentId?.Value;
    }

    private void Apply(AccountDeleted @event)
    {
        _state.Accounts.Remove(@event.AccountId.Value);
    }

    // Operation: validates and normalizes account fields.
    private bool TryNormalizeAccount(string requestedCode, string requestedName, out string code, out string name)
    {
        code = requestedCode.Trim();
        name = requestedName.Trim();

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
        public AccountType Type { get; set; }
        public string? ParentId { get; set; }

        public AccountState Clone()
        {
            return new AccountState
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Type = Type,
                ParentId = ParentId
            };
        }
    }
}