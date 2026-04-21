using Akka.Actor;
using Akka.Hosting;
using Domain.Finances.Queries;
using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Shared;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Notifiers;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Jamaa.Application.Finances;

public class FinanceManagementFacade : IFinanceManagementFacade
{
    private readonly IActorRef _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly IDataChangeNotifier _dataChangeNotifier;
    private readonly Dictionary<string, IObservable<IList<FiscalYearData>>> _fiscalYearStreams = [];

    public FinanceManagementFacade(
        IRequiredActor<CommandProcessor> commandProcessorProvider,
        IQueryProcessor queryProcessor,
        IDataChangeNotifier dataChangeNotifier)
    {
        _commandProcessor = commandProcessorProvider.ActorRef;
        _queryProcessor = queryProcessor;
        _dataChangeNotifier = dataChangeNotifier;
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

    // Operation: returns a reactive stream of fiscal years for an organisation
    public IObservable<IList<FiscalYearData>> GetFiscalYearsStream(string organisationId)
    {
        if (_fiscalYearStreams.TryGetValue(organisationId, out var existingStream))
        {
            return existingStream;
        }

        var financeChanges = Observable.Merge(
            _dataChangeNotifier.Insertions
                .OfType<FiscalYearData>()
                .Where(fiscalYear => fiscalYear.OrganisationId == organisationId)
                .Select(_ => Unit.Default),
            _dataChangeNotifier.Updates
                .OfType<FiscalYearData>()
                .Where(fiscalYear => fiscalYear.OrganisationId == organisationId)
                .Select(_ => Unit.Default),
            _dataChangeNotifier.Deletions
                .OfType<FiscalYearData>()
                .Where(fiscalYear => fiscalYear.OrganisationId == organisationId)
                .Select(_ => Unit.Default),
            _dataChangeNotifier.Insertions
                .OfType<AccountingPeriodData>()
                .Where(period => period.OrganisationId == organisationId)
                .Select(_ => Unit.Default),
            _dataChangeNotifier.Updates
                .OfType<AccountingPeriodData>()
                .Where(period => period.OrganisationId == organisationId)
                .Select(_ => Unit.Default),
            _dataChangeNotifier.Deletions
                .OfType<AccountingPeriodData>()
                .Where(period => period.OrganisationId == organisationId)
                .Select(_ => Unit.Default));

        var stream = Observable.Return(Unit.Default)
            .Merge(financeChanges)
            .SelectMany(_ => Observable.FromAsync(() => GetFiscalYears(organisationId)))
            .DistinctUntilChanged(BuildFiscalYearsSnapshotKey)
            .Replay(1)
            .RefCount();

        _fiscalYearStreams[organisationId] = stream;
        return stream;
    }

    // Operation: builds a deterministic snapshot key for fiscal-year payload deduplication.
    private static string BuildFiscalYearsSnapshotKey(IList<FiscalYearData> fiscalYears)
    {
        var fiscalYearTokens = fiscalYears
            .OrderBy(fiscalYear => fiscalYear.Id)
            .Select(fiscalYear =>
            {
                var periodTokens = (fiscalYear.Periods ?? [])
                    .OrderBy(period => period.SequenceNumber)
                    .ThenBy(period => period.Id)
                    .Select(period =>
                        $"{period.Id}|{period.SequenceNumber}|{period.StartDate:O}|{period.EndDate:O}|{(period.IsLocked ? 1 : 0)}");

                return $"{fiscalYear.Id}|{fiscalYear.OrganisationId}|{fiscalYear.StartDate:O}|{fiscalYear.EndDate:O}|{(fiscalYear.IsLocked ? 1 : 0)}|[{string.Join(";", periodTokens)}]";
            });

        return string.Join("#", fiscalYearTokens);
    }
}


