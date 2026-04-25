using Akka.Actor;
using Akka.Persistence;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Finances.Events;

namespace Jamaa.Application.Finances.Aggregates;

public class AccountingSettingsAggregate : ReceivePersistentActor
{
    private readonly AccountingSettingsState _state = new();

    public AccountingSettingsAggregate(OrganisationId organisationId)
    {
        PersistenceId = $"accounting-settings-{organisationId.Value}";

        RegisterCommandHandlers();
        RegisterEventHandlers();
    }

    public override string PersistenceId { get; }

    public static Props Props(OrganisationId organisationId)
    {
        return new Props(typeof(AccountingSettingsAggregate), [organisationId]);
    }

    private void RegisterCommandHandlers()
    {
        Command<UpdateAccountingSettings>(Handle);
    }

    private void RegisterEventHandlers()
    {
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is AccountingSettingsState state)
            {
                _state.CopyFrom(state);
            }
        });

        Recover<AccountingSettingsUpdated>(Apply);
    }

    // Integration: validates and persists accounting settings configuration for one organisation.
    private void Handle(UpdateAccountingSettings command)
    {
        if (!TryValidateBaseCurrency(command.BaseCurrency, out var currencyError))
        {
            Sender.Tell(currencyError, Self);
            return;
        }

        if (!TryValidateDateFormat(command.DateFormat, out var formatError))
        {
            Sender.Tell(formatError, Self);
            return;
        }

        if (!TryValidateDecimalPrecision(command.DecimalPrecision, out var precisionError))
        {
            Sender.Tell(precisionError, Self);
            return;
        }

        var @event = new AccountingSettingsUpdated(
            command.OrganisationId,
            command.BaseCurrency,
            command.DateFormat,
            command.DecimalPrecision);

        Persist(@event, Apply);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    private void Apply(AccountingSettingsUpdated @event)
    {
        _state.BaseCurrency = @event.BaseCurrency;
        _state.DateFormat = @event.DateFormat;
        _state.DecimalPrecision = @event.DecimalPrecision;
    }

    // Operation: validates that the base currency is a non-empty, known ISO 4217 code.
    public static bool TryValidateBaseCurrency(string currency, out string error)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            error = "Base currency must not be empty.";
            return false;
        }

        if (currency.Length > 10)
        {
            error = "Base currency code must be at most 10 characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // Operation: validates that the date format is a non-empty recognised token.
    public static bool TryValidateDateFormat(string dateFormat, out string error)
    {
        if (string.IsNullOrWhiteSpace(dateFormat))
        {
            error = "Date format must not be empty.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // Operation: validates that decimal precision is within the range [0, 4].
    public static bool TryValidateDecimalPrecision(int precision, out string error)
    {
        if (precision is < 0 or > 4)
        {
            error = "Decimal precision must be between 0 and 4.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void TrySaveSnapshot()
    {
        if (LastSequenceNr % 20 == 0)
        {
            SaveSnapshot(_state.Clone());
        }
    }

    private sealed class AccountingSettingsState
    {
        public string BaseCurrency { get; set; } = "USD";
        public string DateFormat { get; set; } = "DD/MM/YYYY";
        public int DecimalPrecision { get; set; } = 2;

        public AccountingSettingsState Clone() =>
            new() { BaseCurrency = BaseCurrency, DateFormat = DateFormat, DecimalPrecision = DecimalPrecision };

        public void CopyFrom(AccountingSettingsState other)
        {
            BaseCurrency = other.BaseCurrency;
            DateFormat = other.DateFormat;
            DecimalPrecision = other.DecimalPrecision;
        }
    }
}

