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
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Jamaa.Application.Finances;

public class FinanceManagementFacade : IFinanceManagementFacade
{
    private readonly IActorRef _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly ReplaySubject<FiscalYearData> _currentFiscalYears;
    private string? _lastSeededOrganisationId;

    public FinanceManagementFacade(
        IRequiredActor<CommandProcessor> commandProcessorProvider,
        IQueryProcessor queryProcessor,
        IDataChangeNotifier dataChangeNotifier,
        IUserSessionService userSessionService)
    {
        _commandProcessor = commandProcessorProvider.ActorRef;
        _queryProcessor = queryProcessor;

        _currentFiscalYears = new ReplaySubject<FiscalYearData>();
        CurrentFiscalYears = _currentFiscalYears;

        dataChangeNotifier.Insertions
            .OfType<FiscalYearData>()
            .Subscribe(_currentFiscalYears.OnNext);

        FiscalYearUpdated = BuildFiscalYearUpdates(dataChangeNotifier);
        FiscalYearDeleted = dataChangeNotifier.Deletions.OfType<FiscalYearData>();

        // Subscribe after stream fields are initialized and seed from the already-authenticated session.
        userSessionService.UserSessions
            .StartWith(userSessionService.CurrentUserSession)
            .Select(SeedFiscalYearsSafely)
            .Concat()
            .Subscribe(_ => { });
    }

    public IObservable<FiscalYearData> CurrentFiscalYears { get; }

    public IObservable<FiscalYearData> FiscalYearUpdated { get; }

    public IObservable<FiscalYearData> FiscalYearDeleted { get; }

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

    // Integration: reads the materialized fiscal-year view.
    public Task<IList<FiscalYearData>> GetFiscalYears(string organisationId)
    {
        return _queryProcessor.Get(new GetFiscalYearsByOrganisation(OrganisationId.With(organisationId)));
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


