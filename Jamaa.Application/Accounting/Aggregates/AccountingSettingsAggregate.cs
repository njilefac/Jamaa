using Akka.Actor;
using Akka.Persistence;
using Domain.Organisation.Values;
using Jamaa.Application.Accounting.Commands;
using Jamaa.Application.Accounting.Events;
using Jamaa.Application.Accounting.Values;

namespace Jamaa.Application.Accounting.Aggregates;

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
            if (offer.Snapshot is AccountingSettingsState state) _state.CopyFrom(state);
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

        if (!TryValidateAvailableCurrencies(command.AvailableCurrencies, out var currenciesError))
        {
            Sender.Tell(currenciesError, Self);
            return;
        }

        var normalizedCurrencies = NormalizeCurrencies(command.AvailableCurrencies);
        var normalizedBaseCurrency = command.BaseCurrency.Trim().ToUpperInvariant();

        if (!normalizedCurrencies.Any(currency => currency.Code == normalizedBaseCurrency))
        {
            Sender.Tell("Base currency must be part of the available currencies list.", Self);
            return;
        }

        var @event = new AccountingSettingsUpdated(
            command.OrganisationId,
            normalizedBaseCurrency,
            command.DateFormat.Trim(),
            command.DecimalPrecision,
            normalizedCurrencies);

        Persist(@event, Apply);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    private void Apply(AccountingSettingsUpdated @event)
    {
        _state.BaseCurrency = @event.BaseCurrency;
        _state.DateFormat = @event.DateFormat;
        _state.DecimalPrecision = @event.DecimalPrecision;
        _state.AvailableCurrencies = [.. @event.AvailableCurrencies ?? []];
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

    // Operation: validates that one available currency item has both valid code and symbol.
    public static bool TryValidateAvailableCurrency(Currency currency, out string error)
    {
        if (!TryValidateBaseCurrency(currency.Code, out error)) return false;

        if (string.IsNullOrWhiteSpace(currency.Symbol))
        {
            error = "Currency symbol must not be empty.";
            return false;
        }

        if (currency.Symbol.Length > 10)
        {
            error = "Currency symbol must be at most 10 characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // Operation: validates that the managed available currency list is present and has valid entries.
    public static bool TryValidateAvailableCurrencies(IReadOnlyList<Currency> currencies, out string error)
    {
        if (currencies is null || currencies.Count == 0)
        {
            error = "At least one available currency is required.";
            return false;
        }

        var normalizedCodes = new HashSet<string>();
        foreach (var currency in currencies)
        {
            if (!TryValidateAvailableCurrency(currency, out error)) return false;

            normalizedCodes.Add(currency.Code.Trim().ToUpperInvariant());
        }

        if (normalizedCodes.Count == 0)
        {
            error = "At least one available currency is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // Operation: normalizes currencies to uppercase code, trimmed symbol, and distinct by code in stable order.
    public static List<Currency> NormalizeCurrencies(IReadOnlyList<Currency> currencies)
    {
        return currencies
            .Select(currency => new Currency(
                currency.Code.Trim().ToUpperInvariant(),
                currency.Symbol.Trim()))
            .Where(currency => !string.IsNullOrWhiteSpace(currency.Code) && !string.IsNullOrWhiteSpace(currency.Symbol))
            .GroupBy(currency => currency.Code)
            .Select(group => group.First())
            .OrderBy(currency => currency.Code)
            .ToList();
    }

    private void TrySaveSnapshot()
    {
        if (LastSequenceNr % 20 == 0) SaveSnapshot(_state.Clone());
    }

    private sealed class AccountingSettingsState
    {
        public string BaseCurrency { get; set; } = "USD";
        public string DateFormat { get; set; } = "DD/MM/YYYY";
        public int DecimalPrecision { get; set; } = 2;

        public List<Currency> AvailableCurrencies { get; set; } =
        [
            new("USD", "$"),
            new("KES", "KSh"),
            new("EUR", "EUR")
        ];

        public AccountingSettingsState Clone()
        {
            return new AccountingSettingsState
            {
                BaseCurrency = BaseCurrency,
                DateFormat = DateFormat,
                DecimalPrecision = DecimalPrecision,
                AvailableCurrencies = [.. AvailableCurrencies]
            };
        }

        public void CopyFrom(AccountingSettingsState other)
        {
            BaseCurrency = other.BaseCurrency;
            DateFormat = other.DateFormat;
            DecimalPrecision = other.DecimalPrecision;
            AvailableCurrencies = [.. other.AvailableCurrencies];
        }
    }
}