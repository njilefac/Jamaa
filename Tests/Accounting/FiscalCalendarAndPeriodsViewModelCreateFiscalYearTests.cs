using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Services.Notifications;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public sealed class FiscalCalendarAndPeriodsViewModelCreateFiscalYearTests : IDisposable
{
    private const string OrgId = "org-fiscal-create-tests";
    private readonly Subject<FiscalYearData> _currentFiscalYears;

    private readonly IFinanceManagementFacade _financeFacade;
    private readonly Subject<FiscalYearData> _fiscalYearDeleted;
    private readonly Subject<FiscalYearData> _fiscalYearUpdated;
    private readonly FiscalCalendarAndPeriodsViewModel _viewModel;

    public FiscalCalendarAndPeriodsViewModelCreateFiscalYearTests()
    {
        _financeFacade = Substitute.For<IFinanceManagementFacade>();
        _currentFiscalYears = new Subject<FiscalYearData>();
        _fiscalYearUpdated = new Subject<FiscalYearData>();
        _fiscalYearDeleted = new Subject<FiscalYearData>();

        _financeFacade.CurrentFiscalYears.Returns(_currentFiscalYears.AsObservable());
        _financeFacade.FiscalYearUpdated.Returns(_fiscalYearUpdated.AsObservable());
        _financeFacade.FiscalYearDeleted.Returns(_fiscalYearDeleted.AsObservable());
        _financeFacade.CreateFiscalYear(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);

        var organisation = new OrganisationData { Id = OrgId, Name = "Test Organisation" };
        var userSession = new UserSession(true, "admin", Guid.NewGuid(), organisation);
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(userSession);
        var notificationService = Substitute.For<INotificationService>();

        _viewModel = new FiscalCalendarAndPeriodsViewModel(_financeFacade, userSessionService, notificationService);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _currentFiscalYears.Dispose();
        _fiscalYearUpdated.Dispose();
        _fiscalYearDeleted.Dispose();
    }

    [Fact]
    public async Task AddFiscalYear_ReloadsCreatedYearWithGeneratedPeriods_WhenCurrentStreamItemHasNoPeriods()
    {
        await SeedExistingFiscalYear();

        _financeFacade.GetFiscalYears(OrgId).Returns(Task.FromResult<IList<FiscalYearData>>(new List<FiscalYearData>
        {
            CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
            CreateFiscalYearData(
                new DateTime(2027, 1, 1),
                new DateTime(2027, 12, 31),
                [CreateAccountingPeriodData("2027", 1, new DateTime(2027, 1, 1), new DateTime(2027, 1, 31))])
        }));

        var addTask = _viewModel.AddFiscalYearCommand.ExecuteAsync(null);
        await Task.Delay(60);

        _currentFiscalYears.OnNext(CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), []));
        await addTask;

        _viewModel.FiscalYears.Count.ShouldBe(2);
        _viewModel.SelectedFiscalYear.ShouldNotBeNull();
        _viewModel.SelectedFiscalYear!.StartDate.Year.ShouldBe(2027);
        _viewModel.SelectedFiscalYear.Periods.Count.ShouldBe(1);
        _viewModel.SelectedFiscalYear.Periods[0].StartDate.Date.ShouldBe(new DateTime(2027, 1, 1));
    }

    [Fact]
    public async Task AddFiscalYear_DoesNotLosePeriods_WhenLateCurrentSnapshotArrivesWithoutPeriods()
    {
        await SeedExistingFiscalYear();

        _financeFacade.GetFiscalYears(OrgId).Returns(Task.FromResult<IList<FiscalYearData>>(new List<FiscalYearData>
        {
            CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
            CreateFiscalYearData(
                new DateTime(2027, 1, 1),
                new DateTime(2027, 12, 31),
                [CreateAccountingPeriodData("2027", 1, new DateTime(2027, 1, 1), new DateTime(2027, 1, 31))])
        }));

        var addTask = _viewModel.AddFiscalYearCommand.ExecuteAsync(null);
        await Task.Delay(60);

        _currentFiscalYears.OnNext(CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), []));
        await addTask;

        _viewModel.SelectedFiscalYear.ShouldNotBeNull();
        _viewModel.SelectedFiscalYear!.Periods.Count.ShouldBe(1);

        // Simulate a late stale current-state replay that still has no periods.
        _currentFiscalYears.OnNext(CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), []));
        await Task.Delay(120);

        _viewModel.SelectedFiscalYear.ShouldNotBeNull();
        _viewModel.SelectedFiscalYear!.StartDate.Year.ShouldBe(2027);
        _viewModel.SelectedFiscalYear.Periods.Count.ShouldBe(1);
        _viewModel.SelectedFiscalYear.Periods[0].StartDate.Date.ShouldBe(new DateTime(2027, 1, 1));
    }

    private async Task SeedExistingFiscalYear()
    {
        _currentFiscalYears.OnNext(CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []));
        await Task.Delay(120);
    }

    private static FiscalYearData CreateFiscalYearData(DateTime startDate, DateTime endDate,
        IList<AccountingPeriodData> periods)
    {
        return new FiscalYearData
        {
            Id = Guid.NewGuid().ToString(),
            OrganisationId = OrgId,
            StartDate = startDate,
            EndDate = endDate,
            IsLocked = false,
            Periods = periods
        };
    }

    private static AccountingPeriodData CreateAccountingPeriodData(string suffix, int sequenceNumber,
        DateTime startDate, DateTime endDate)
    {
        return new AccountingPeriodData
        {
            Id = $"period-{suffix}-{sequenceNumber:00}",
            OrganisationId = OrgId,
            FiscalYearId = $"fy-{suffix}",
            SequenceNumber = sequenceNumber,
            StartDate = startDate,
            EndDate = endDate,
            IsLocked = false
        };
    }
}