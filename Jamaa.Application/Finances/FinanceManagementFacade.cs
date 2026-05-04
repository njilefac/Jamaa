using Akka.Actor;
using Akka.Hosting;
using Domain.Finances.Queries;
using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Shared;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Notifiers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AppCurrency = Jamaa.Application.Finances.Values.Currency;

namespace Jamaa.Application.Finances;

public class FinanceManagementFacade : IFinanceManagementFacade
{
    private readonly IActorRef _commandProcessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReplaySubject<FiscalYearData> _currentFiscalYears;
    private readonly BehaviorSubject<AccountingSettingsData?> _currentAccountingSettings;
    private string? _lastSeededOrganisationId;

    public FinanceManagementFacade(
        IRequiredActor<CommandProcessor> commandProcessorProvider,
        IServiceScopeFactory scopeFactory,
        IDataChangeNotifier dataChangeNotifier,
        IUserSessionService userSessionService)
    {
        _commandProcessor = commandProcessorProvider.ActorRef;
        _scopeFactory = scopeFactory;

        _currentFiscalYears = new ReplaySubject<FiscalYearData>();
        CurrentFiscalYears = _currentFiscalYears;

        dataChangeNotifier.Insertions
            .OfType<FiscalYearData>()
            .Subscribe(_currentFiscalYears.OnNext);

        AccountCreated = dataChangeNotifier.Insertions.OfType<AccountData>();
        AccountUpdated = dataChangeNotifier.Updates.OfType<AccountData>();
        AccountDeleted = dataChangeNotifier.Deletions.OfType<AccountData>();
        AccountDeactivated = dataChangeNotifier.Updates.OfType<AccountData>().Where(a => !a.IsActive);
        AccountReactivated = dataChangeNotifier.Updates.OfType<AccountData>().Where(a => a.IsActive);

        FiscalYearUpdated = BuildFiscalYearUpdates(dataChangeNotifier);
        FiscalYearDeleted = dataChangeNotifier.Deletions.OfType<FiscalYearData>();

        _currentAccountingSettings = new BehaviorSubject<AccountingSettingsData?>(null);
        CurrentAccountingSettings = _currentAccountingSettings;
        AccountingSettingsUpdated = BuildAccountingSettingsUpdates(dataChangeNotifier);

        AccountingSettingsUpdated
            .Subscribe(_currentAccountingSettings.OnNext);

        // Subscribe after stream fields are initialized and seed from the already-authenticated session.
        userSessionService.UserSessions
            .StartWith(userSessionService.CurrentUserSession)
            .Select(SeedFiscalYearsSafely)
            .Concat()
            .Subscribe(_ => { });

        userSessionService.UserSessions
            .StartWith(userSessionService.CurrentUserSession)
            .Select(SeedAccountingSettingsSafely)
            .Concat()
            .Subscribe(_ => { });
    }

    public IObservable<FiscalYearData> CurrentFiscalYears { get; }
    public IObservable<FiscalYearData> FiscalYearUpdated { get; }
    public IObservable<FiscalYearData> FiscalYearDeleted { get; }
    public IObservable<AccountData> AccountCreated { get; }
    public IObservable<AccountData> AccountUpdated { get; }
    public IObservable<AccountData> AccountDeleted { get; }
    public IObservable<AccountData> AccountDeactivated { get; }
    public IObservable<AccountData> AccountReactivated { get; }
    public IObservable<AccountingSettingsData?> CurrentAccountingSettings { get; }
    public IObservable<AccountingSettingsData> AccountingSettingsUpdated { get; }

    // Integration: builds a safe async seeding step for one session emission.
    private static IObservable<Unit> SeedFiscalYearsSafely(UserSession? session, Func<UserSession?, Task> seedFiscalYears)
    {
        return Observable.FromAsync(() => seedFiscalYears(session))
            .Catch<Unit, Exception>(_ => Observable.Empty<Unit>());
    }

    // Integration: builds the seeding workflow for one session while keeping the stream alive on failure.
    private IObservable<Unit> SeedFiscalYearsSafely(UserSession? session)
    {
        return SeedFiscalYearsSafely(session, InitializeCurrentFiscalYearsAsync);
    }

    // Integration: seeds the current-fiscal-year stream from the active session organisation.
    private async Task InitializeCurrentFiscalYearsAsync(UserSession? session)
    {
        var organisationId = session?.Organisation?.Id;
        if (string.IsNullOrWhiteSpace(organisationId))
        {
            return;
        }

        // Guard: skip re-seeding if the same session is re-emitted in succession.
        if (organisationId == _lastSeededOrganisationId)
        {
            return;
        }

        var existingFiscalYears = await GetFiscalYears(organisationId);
        foreach (var fiscalYear in existingFiscalYears)
        {
            _currentFiscalYears.OnNext(fiscalYear);
        }

        _lastSeededOrganisationId = organisationId;
    }

    // Integration: wraps accounting settings seeding safely so stream errors don't terminate the session pipeline.
    private IObservable<Unit> SeedAccountingSettingsSafely(UserSession? session)
    {
        return Observable.FromAsync(() => InitializeCurrentAccountingSettingsAsync(session))
            .Catch<Unit, Exception>(_ => Observable.Empty<Unit>());
    }

    // Integration: seeds the current-accounting-settings stream from the active session organisation.
    private async Task InitializeCurrentAccountingSettingsAsync(UserSession? session)
    {
        var organisationId = session?.Organisation?.Id;
        if (string.IsNullOrWhiteSpace(organisationId))
        {
            return;
        }

        var existing = await GetAccountingSettings(organisationId);
        _currentAccountingSettings.OnNext(existing);
    }

    // Integration: dispatches intent to the finance aggregate stream.
    public Task CreateFiscalYear(string organisationId, DateTime startDate, DateTime endDate, bool isLocked)
    {
        var command = new CreateFiscalYear(
            OrganisationId.With(organisationId),
            FiscalYearId.New(),
            startDate,
            endDate,
            isLocked);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches account creation intent.
    public Task CreateAccount(string organisationId, string code, string name, string description, AccountType type, string? parentAccountId)
    {
        var command = new CreateAccount(
            OrganisationId.With(organisationId),
            AccountId.With(Guid.NewGuid()),
            code,
            name,
            type,
            string.IsNullOrWhiteSpace(parentAccountId) ? null : AccountId.With(parentAccountId),
            description);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches account update intent.
    public Task UpdateAccount(string organisationId, string accountId, string code, string name, string description, AccountType type, string? parentAccountId)
    {
        var command = new UpdateAccount(
            OrganisationId.With(organisationId),
            AccountId.With(accountId),
            code,
            name,
            type,
            string.IsNullOrWhiteSpace(parentAccountId) ? null : AccountId.With(parentAccountId),
            description);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches account deletion intent.
    public Task DeleteAccount(string organisationId, string accountId)
    {
        var command = new DeleteAccount(
            OrganisationId.With(organisationId),
            AccountId.With(accountId));

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches account deactivation intent.
    public Task DeactivateAccount(string organisationId, string accountId)
    {
        var command = new DeactivateAccount(
            OrganisationId.With(organisationId),
            AccountId.With(accountId));

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches account reactivation intent.
    public Task ReactivateAccount(string organisationId, string accountId)
    {
        var command = new ReactivateAccount(
            OrganisationId.With(organisationId),
            AccountId.With(accountId));

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches fiscal-year update intent.
    public Task UpdateFiscalYear(string organisationId, string fiscalYearId, DateTime startDate, DateTime endDate, bool isLocked)
    {
        var command = new UpdateFiscalYear(
            OrganisationId.With(organisationId),
            FiscalYearId.With(fiscalYearId),
            startDate,
            endDate,
            isLocked);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches fiscal-year delete intent.
    public Task DeleteFiscalYear(string organisationId, string fiscalYearId)
    {
        var command = new DeleteFiscalYear(
            OrganisationId.With(organisationId),
            FiscalYearId.With(fiscalYearId));

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches period creation intent for a fiscal year.
    public Task CreateAccountingPeriod(string organisationId, string fiscalYearId, int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked)
    {
        var command = new CreateAccountingPeriod(
            OrganisationId.With(organisationId),
            FiscalYearId.With(fiscalYearId),
            AccountingPeriodId.New(),
            sequenceNumber,
            startDate,
            endDate,
            isLocked);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches period update intent.
    public Task UpdateAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId, int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked)
    {
        var command = new UpdateAccountingPeriod(
            OrganisationId.With(organisationId),
            FiscalYearId.With(fiscalYearId),
            AccountingPeriodId.With(accountingPeriodId),
            sequenceNumber,
            startDate,
            endDate,
            isLocked);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches period delete intent.
    public Task DeleteAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId)
    {
        var command = new DeleteAccountingPeriod(
            OrganisationId.With(organisationId),
            FiscalYearId.With(fiscalYearId),
            AccountingPeriodId.With(accountingPeriodId));

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches accounting settings update intent.
    public Task UpdateAccountingSettings(string organisationId, string baseCurrency, string dateFormat, int decimalPrecision, IReadOnlyList<AppCurrency> availableCurrencies)
    {
        var command = new UpdateAccountingSettings(
            OrganisationId.With(organisationId),
            baseCurrency,
            dateFormat,
            decimalPrecision,
            availableCurrencies?.ToList() ?? []);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: reads the materialized fiscal-year view.
    public Task<IList<AccountData>> GetAccounts(string organisationId)
    {
        return QueryAsync(queryProcessor => queryProcessor.Get(new GetAccountsByOrganisation(OrganisationId.With(organisationId))));
    }

    // Integration: reads the materialized fiscal-year view.
    public Task<IList<FiscalYearData>> GetFiscalYears(string organisationId)
    {
        return QueryAsync(queryProcessor => queryProcessor.Get(new GetFiscalYearsByOrganisation(OrganisationId.With(organisationId))));
    }

    // Integration: reads the materialized accounting settings view.
    public Task<AccountingSettingsData?> GetAccountingSettings(string organisationId)
    {
        return QueryAsync(queryProcessor => queryProcessor.Get(new GetAccountingSettingsByOrganisation(OrganisationId.With(organisationId))));
    }

    // Operation: executes one read-model query in an isolated DI scope to avoid sharing DbContext across concurrent calls.
    private async Task<T> QueryAsync<T>(Func<IQueryProcessor, Task<T>> query)
    {
        const int maxAttempts = 4;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
                return await query(queryProcessor);
            }
            catch (SqliteException ex) when (IsTransientSqliteLock(ex) && attempt < maxAttempts)
            {
                // SQLite can briefly lock during projection writes; retry with a short backoff.
                await Task.Delay(TimeSpan.FromMilliseconds(75 * attempt));
            }
        }

        // Final attempt - let any exception bubble with full context.
        using (var scope = _scopeFactory.CreateScope())
        {
            var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
            return await query(queryProcessor);
        }
    }

    private static bool IsTransientSqliteLock(SqliteException ex)
    {
        return ex.SqliteErrorCode is 5 or 6;
    }

    // Operation: emits canonical accounting-settings snapshots by reloading the read model after settings/currency row changes.
    private IObservable<AccountingSettingsData> BuildAccountingSettingsUpdates(IDataChangeNotifier dataChangeNotifier)
    {
        var settingsOrganisationChanges = Observable.Merge(
                dataChangeNotifier.Insertions.OfType<AccountingSettingsData>(),
                dataChangeNotifier.Updates.OfType<AccountingSettingsData>())
            .Select(settings => settings.OrganisationId);

        var currencyOrganisationChanges = Observable.Merge(
                dataChangeNotifier.Insertions.OfType<AccountingAvailableCurrencyData>(),
                dataChangeNotifier.Updates.OfType<AccountingAvailableCurrencyData>(),
                dataChangeNotifier.Deletions.OfType<AccountingAvailableCurrencyData>())
            .Select(currency => currency.OrganisationId);

        var triggeredReloads = settingsOrganisationChanges
            .Merge(currencyOrganisationChanges)
            .Where(organisationId => !string.IsNullOrWhiteSpace(organisationId))
            .GroupBy(organisationId => organisationId)
            .SelectMany(group => group
                .Throttle(TimeSpan.FromMilliseconds(80))
                .SelectMany(organisationId => Observable.FromAsync(() => GetAccountingSettings(organisationId))
                    .Catch<AccountingSettingsData?, Exception>(_ => Observable.Empty<AccountingSettingsData?>())));

        return triggeredReloads
            .Where(settings => settings is not null)
            .Select(settings => settings!);
    }

    // Operation: builds a deterministic key for accounting-settings deduplication across row and list changes.
    private static string BuildAccountingSettingsSnapshotKey(AccountingSettingsData settings)
    {
        var currencies = (settings.AvailableCurrencies ?? [])
            .OrderBy(currency => currency.CurrencyCode)
            .Select(currency => $"{currency.CurrencyCode}:{currency.CurrencySymbol}");

        return $"{settings.OrganisationId}|{settings.BaseCurrency}|{settings.DateFormat}|{settings.DecimalPrecision}|[{string.Join(';', currencies)}]";
    }

    // Operation: merges direct fiscal-year updates with period-driven fiscal-year refreshes.
    private IObservable<FiscalYearData> BuildFiscalYearUpdates(IDataChangeNotifier dataChangeNotifier)
    {
        var fiscalYearUpdates = dataChangeNotifier.Updates.OfType<FiscalYearData>();

        var periodDrivenUpdates = Observable.Merge(
                dataChangeNotifier.Insertions.OfType<AccountingPeriodData>(),
                dataChangeNotifier.Updates.OfType<AccountingPeriodData>(),
                dataChangeNotifier.Deletions.OfType<AccountingPeriodData>())
            .Select(period => (period.OrganisationId, period.FiscalYearId))
            .GroupBy(period => $"{period.OrganisationId}:{period.FiscalYearId}")
            .SelectMany(group => group
                .Throttle(TimeSpan.FromMilliseconds(120))
                .SelectMany(period => Observable.FromAsync(() => GetFiscalYearById(period.OrganisationId, period.FiscalYearId)))
                .Where(fiscalYear => fiscalYear is not null)
                .Select(fiscalYear => fiscalYear!));

        return fiscalYearUpdates
            .Merge(periodDrivenUpdates)
            .DistinctUntilChanged(BuildFiscalYearSnapshotKey);
    }

    // Operation: resolves one fiscal year by identifier from the organisation read model.
    private async Task<FiscalYearData?> GetFiscalYearById(string organisationId, string fiscalYearId)
    {
        var fiscalYears = await GetFiscalYears(organisationId);
        return fiscalYears.FirstOrDefault(fiscalYear => fiscalYear.Id == fiscalYearId);
    }

    // Operation: builds a deterministic key for fiscal-year payload deduplication.
    private static string BuildFiscalYearSnapshotKey(FiscalYearData fiscalYear)
    {
        var periodTokens = (fiscalYear.Periods ?? [])
            .OrderBy(period => period.SequenceNumber)
            .ThenBy(period => period.Id)
            .Select(period =>
                $"{period.Id}|{period.SequenceNumber}|{period.StartDate:O}|{period.EndDate:O}|{(period.IsLocked ? 1 : 0)}");

        return $"{fiscalYear.Id}|{fiscalYear.OrganisationId}|{fiscalYear.StartDate:O}|{fiscalYear.EndDate:O}|{(fiscalYear.IsLocked ? 1 : 0)}|[{string.Join(";", periodTokens)}]";
    }
}
