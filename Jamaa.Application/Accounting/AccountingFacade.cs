using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Domain.Accounting.Queries;
using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Accounting.Commands;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Shared;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Queries.Finances;
using Jamaa.Data.Notifiers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using DataModels = Jamaa.Data.Models.Finances;

namespace Jamaa.Application.Accounting;

using AccountData = AccountData;
using AccountingSettingsData = AccountingSettingsData;

public class AccountingFacade : IAccountingFacade
{
    private readonly IActorRef _commandProcessor;
    private readonly BehaviorSubject<AccountingSettingsData?> _currentAccountingSettings;
    private readonly BehaviorSubject<FiscalCalendarData?> _currentFiscalCalendar;
    private readonly IServiceScopeFactory _scopeFactory;
    private string? _lastSeededOrganisationId;

    public AccountingFacade(
        IRequiredActor<CommandProcessor> commandProcessorProvider,
        IServiceScopeFactory scopeFactory,
        IDataChangeNotifier dataChangeNotifier,
        IUserSessionService userSessionService)
    {
        _commandProcessor = commandProcessorProvider.ActorRef;
        _scopeFactory = scopeFactory;

        _currentFiscalCalendar = new BehaviorSubject<FiscalCalendarData?>(null);
        CurrentFiscalCalendar = _currentFiscalCalendar
            .Where(calendar => calendar is not null)
            .Select(calendar => calendar!);

        FiscalCalendarUpdated = BuildFiscalCalendarUpdates(dataChangeNotifier);

        // Keep the current-calendar snapshot up-to-date whenever data changes.
        FiscalCalendarUpdated
            .Subscribe(calendar => _currentFiscalCalendar.OnNext(calendar));

        AccountCreated = dataChangeNotifier.Insertions.OfType<DataModels.AccountData>()
            .Select(account => account.ToPresentationModel());
        AccountUpdated = dataChangeNotifier.Updates.OfType<DataModels.AccountData>()
            .Select(account => account.ToPresentationModel());
        AccountDeleted = dataChangeNotifier.Deletions.OfType<DataModels.AccountData>()
            .Select(account => account.ToPresentationModel());
        AccountDeactivated = dataChangeNotifier.Updates.OfType<DataModels.AccountData>().Where(a => !a.IsActive)
            .Select(account => account.ToPresentationModel());
        AccountReactivated = dataChangeNotifier.Updates.OfType<DataModels.AccountData>().Where(a => a.IsActive)
            .Select(account => account.ToPresentationModel());
        AccountOpeningBalanceSet = dataChangeNotifier.Updates.OfType<DataModels.AccountingPeriodBalanceData>()
            .Select(balance => balance.ToPresentationModel());

        _currentAccountingSettings = new BehaviorSubject<AccountingSettingsData?>(null);
        CurrentAccountingSettings = _currentAccountingSettings;
        AccountingSettingsUpdated = BuildAccountingSettingsUpdates(dataChangeNotifier);

        AccountingSettingsUpdated
            .Subscribe(_currentAccountingSettings.OnNext);

        // Subscribe after stream fields are initialized and seed from the already-authenticated session.
        userSessionService.UserSessions
            .StartWith(userSessionService.CurrentUserSession)
            .Select(SeedFiscalCalendarSafely)
            .Concat()
            .Subscribe(_ => { });

        userSessionService.UserSessions
            .StartWith(userSessionService.CurrentUserSession)
            .Select(SeedAccountingSettingsSafely)
            .Concat()
            .Subscribe(_ => { });
    }

    public IObservable<FiscalCalendarData> CurrentFiscalCalendar { get; }
    public IObservable<FiscalCalendarData> FiscalCalendarUpdated { get; }
    public IObservable<AccountData> AccountCreated { get; }
    public IObservable<AccountData> AccountUpdated { get; }
    public IObservable<AccountData> AccountDeleted { get; }
    public IObservable<AccountData> AccountDeactivated { get; }
    public IObservable<AccountData> AccountReactivated { get; }
    public IObservable<AccountingPeriodBalanceData> AccountOpeningBalanceSet { get; }
    public IObservable<AccountingSettingsData?> CurrentAccountingSettings { get; }
    public IObservable<AccountingSettingsData> AccountingSettingsUpdated { get; }

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
    public Task CreateAccount(string organisationId, string code, string name, string description, AccountType type,
        string? parentAccountId)
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
    public Task UpdateAccount(string organisationId, string accountId, string code, string name, string description,
        AccountType type, string? parentAccountId)
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

    public Task SetAccountOpeningBalance(string organisationId, string accountId, string fiscalYearId,
        string accountingPeriodId, decimal openingBalance)
    {
        var command = new SetAccountOpeningBalance(
            OrganisationId.With(organisationId),
            AccountId.With(accountId),
            new FiscalYearId(fiscalYearId),
            new AccountingPeriodId(accountingPeriodId),
            openingBalance);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatches fiscal-year update intent.
    public Task UpdateFiscalYear(string organisationId, string fiscalYearId, DateTime startDate, DateTime endDate,
        bool isLocked)
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
    public Task CreateAccountingPeriod(string organisationId, string fiscalYearId, int sequenceNumber,
        DateTime startDate, DateTime endDate, bool isLocked)
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
    public Task UpdateAccountingPeriod(string organisationId, string fiscalYearId, string accountingPeriodId,
        int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked)
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
    public Task UpdateAccountingSettings(string organisationId, string baseCurrency, string dateFormat,
        int decimalPrecision, IReadOnlyList<Currency> availableCurrencies)
    {
        var command = new UpdateAccountingSettings(
            OrganisationId.With(organisationId),
            baseCurrency,
            dateFormat,
            decimalPrecision,
            [.. availableCurrencies]);

        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: reads the materialized chart-of-accounts view for the given organisation.
    public async Task<ChartOfAccountsData> GetChartOfAccounts(string organisationId)
    {
        var accounts = await QueryAsync(queryProcessor =>
            queryProcessor.Get(new GetAccountsByOrganisation(OrganisationId.With(organisationId))));
        return accounts.ToChartOfAccountsReadModel(organisationId);
    }

    // Integration: reads the full fiscal calendar (all fiscal years with their accounting periods) for the given organisation.
    public async Task<FiscalCalendarData> GetFiscalCalendar(string organisationId)
    {
        var fiscalYears = await QueryAsync(queryProcessor =>
            queryProcessor.Get(new GetFiscalYearsByOrganisation(OrganisationId.With(organisationId))));
        return new FiscalCalendarData
        {
            OrganisationId = organisationId,
            FiscalYears = fiscalYears.Select(fiscalYear => fiscalYear.ToPresentationModel()).ToList()
        };
    }

    // Integration: reads the materialized accounting settings view.
    public async Task<AccountingSettingsData?> GetAccountingSettings(string organisationId)
    {
        var settings = await QueryAsync(queryProcessor =>
            queryProcessor.Get(new GetAccountingSettingsByOrganisation(OrganisationId.With(organisationId))));
        return settings?.ToPresentationModel();
    }

    public async Task<decimal> GetAccountOpeningBalance(string organisationId, string accountId, string fiscalYearId,
        string accountingPeriodId)
    {
        using var scope = _scopeFactory.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<IAccountQueryHandler>();
        return await queryHandler.GetOpeningBalance(organisationId, accountId, fiscalYearId, accountingPeriodId);
    }

    public async Task<bool> IsAccountingSetupComplete(string organisationId)
    {
        var fiscalCalendarTask = GetFiscalCalendar(organisationId);
        var settingsTask = GetAccountingSettings(organisationId);
        var chartOfAccountsTask = GetChartOfAccounts(organisationId);
        var openingBalancesTask = HasOpeningBalances(organisationId);

        await Task.WhenAll(fiscalCalendarTask, settingsTask, chartOfAccountsTask, openingBalancesTask);

        var fiscalCalendar = await fiscalCalendarTask;
        var settings = await settingsTask;
        var chartOfAccounts = await chartOfAccountsTask;
        var hasOpeningBalances = await openingBalancesTask;

        return fiscalCalendar.FiscalYears.Count > 0
               && settings is { AvailableCurrencies.Count: > 0 }
               && chartOfAccounts.Accounts.Count > 0
               && hasOpeningBalances;
    }

    // Operation: checks whether any opening balance rows exist for the organisation.
    private async Task<bool> HasOpeningBalances(string organisationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<IAccountQueryHandler>();
        return await queryHandler.HasOpeningBalances(organisationId);
    }

    // Integration: wraps fiscal-calendar seeding safely so stream errors don't terminate the session pipeline.
    private IObservable<Unit> SeedFiscalCalendarSafely(UserSession? session)
    {
        return Observable.FromAsync(() => InitializeCurrentFiscalCalendarAsync(session))
            .Catch<Unit, Exception>(_ => Observable.Empty<Unit>());
    }

    // Integration: seeds the current-fiscal-calendar stream from the active session organisation.
    private async Task InitializeCurrentFiscalCalendarAsync(UserSession? session)
    {
        var organisationId = session?.Organisation?.Id;
        if (string.IsNullOrWhiteSpace(organisationId)) return;

        // Guard: skip re-seeding if the same session is re-emitted in succession.
        if (organisationId == _lastSeededOrganisationId) return;

        var calendar = await GetFiscalCalendar(organisationId);
        _currentFiscalCalendar.OnNext(calendar);

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
        if (string.IsNullOrWhiteSpace(organisationId)) return;

        var existing = await GetAccountingSettings(organisationId);
        _currentAccountingSettings.OnNext(existing);
    }

    // Operation: executes one read-model query in an isolated DI scope to avoid sharing DbContext across concurrent calls.
    private async Task<T> QueryAsync<T>(Func<IQueryProcessor, Task<T>> query)
    {
        const int maxAttempts = 4;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
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
        var settingsOrganisationChanges = dataChangeNotifier.Insertions.OfType<DataModels.AccountingSettingsData>()
            .Merge(dataChangeNotifier.Updates.OfType<DataModels.AccountingSettingsData>())
            .Select(settings => settings.OrganisationId);

        var currencyOrganisationChanges = Observable.Merge(
                dataChangeNotifier.Insertions.OfType<DataModels.AccountingAvailableCurrencyData>(),
                dataChangeNotifier.Updates.OfType<DataModels.AccountingAvailableCurrencyData>(),
                dataChangeNotifier.Deletions.OfType<DataModels.AccountingAvailableCurrencyData>())
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

    // Operation: reloads the full fiscal calendar whenever any fiscal-year or period row changes.
    private IObservable<FiscalCalendarData> BuildFiscalCalendarUpdates(IDataChangeNotifier dataChangeNotifier)
    {
        var fiscalYearChanges = Observable.Merge(
                dataChangeNotifier.Insertions.OfType<DataModels.FiscalYearData>(),
                dataChangeNotifier.Updates.OfType<DataModels.FiscalYearData>(),
                dataChangeNotifier.Deletions.OfType<DataModels.FiscalYearData>())
            .Select(fiscalYear => fiscalYear.OrganisationId);

        var periodChanges = Observable.Merge(
                dataChangeNotifier.Insertions.OfType<DataModels.AccountingPeriodData>(),
                dataChangeNotifier.Updates.OfType<DataModels.AccountingPeriodData>(),
                dataChangeNotifier.Deletions.OfType<DataModels.AccountingPeriodData>())
            .Select(period => period.OrganisationId);

        return fiscalYearChanges
            .Merge(periodChanges)
            .Where(organisationId => !string.IsNullOrWhiteSpace(organisationId))
            .GroupBy(organisationId => organisationId)
            .SelectMany(group => group
                .Throttle(TimeSpan.FromMilliseconds(120))
                .SelectMany(organisationId =>
                    Observable.FromAsync(() => GetFiscalCalendar(organisationId))
                        .Catch<FiscalCalendarData, Exception>(_ => Observable.Empty<FiscalCalendarData>())))
            .DistinctUntilChanged(BuildFiscalCalendarSnapshotKey);
    }

    // Operation: builds a deterministic key for fiscal-calendar payload deduplication.
    private static string BuildFiscalCalendarSnapshotKey(FiscalCalendarData calendar)
    {
        var fiscalYearTokens = calendar.FiscalYears
            .OrderBy(fiscalYear => fiscalYear.StartDate)
            .ThenBy(fiscalYear => fiscalYear.Id)
            .Select(fiscalYear =>
            {
                var periodTokens = fiscalYear.Periods
                    .OrderBy(period => period.SequenceNumber)
                    .ThenBy(period => period.Id)
                    .Select(period =>
                        $"{period.Id}|{period.SequenceNumber}|{period.StartDate:O}|{period.EndDate:O}|{(period.IsLocked ? 1 : 0)}");

                return
                    $"{fiscalYear.Id}|{fiscalYear.StartDate:O}|{fiscalYear.EndDate:O}|{(fiscalYear.IsLocked ? 1 : 0)}|[{string.Join(";", periodTokens)}]";
            });

        return $"{calendar.OrganisationId}|[{string.Join(";", fiscalYearTokens)}]";
    }
}

