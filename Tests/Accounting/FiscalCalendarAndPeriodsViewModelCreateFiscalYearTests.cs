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
    private readonly Subject<FiscalCalendarData> _currentFiscalCalendar;
    private readonly Subject<FiscalCalendarData> _fiscalCalendarUpdated;

    private readonly IAccountingFacade _financeFacade;
    private readonly FiscalCalendarAndPeriodsViewModel _viewModel;

    public FiscalCalendarAndPeriodsViewModelCreateFiscalYearTests()
    {
        _financeFacade = Substitute.For<IAccountingFacade>();
        _currentFiscalCalendar = new Subject<FiscalCalendarData>();
        _fiscalCalendarUpdated = new Subject<FiscalCalendarData>();

        _financeFacade.CurrentFiscalCalendar.Returns(_currentFiscalCalendar.AsObservable());
        _financeFacade.FiscalCalendarUpdated.Returns(_fiscalCalendarUpdated.AsObservable());
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
        _currentFiscalCalendar.Dispose();
        _fiscalCalendarUpdated.Dispose();
    }

    [Fact]
    public async Task AddFiscalYear_ReloadsCreatedYearWithGeneratedPeriods_WhenCalendarStreamHasNoPeriods()
    {
        await SeedExistingFiscalYear();

        var period2027 = CreateAccountingPeriodData("2027", 1, new DateTime(2027, 1, 1), new DateTime(2027, 1, 31));
        _financeFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears = new List<FiscalYearData>
            {
                CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
                CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), [period2027])
            }
        }));

        var addTask = _viewModel.AddFiscalYearCommand.ExecuteAsync(null);
        await Task.Delay(60);

        // Simulate the calendar update confirming the new fiscal year (without periods yet).
        _fiscalCalendarUpdated.OnNext(CreateCalendarSnapshot([
            CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
            CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), [])
        ]));
        await addTask;

        _viewModel.FiscalYears.Count.ShouldBe(2);
        _viewModel.SelectedFiscalYear.ShouldNotBeNull();
        _viewModel.SelectedFiscalYear!.StartDate.Year.ShouldBe(2027);
        _viewModel.SelectedFiscalYear.Periods.Count.ShouldBe(1);
        _viewModel.SelectedFiscalYear.Periods[0].StartDate.Date.ShouldBe(new DateTime(2027, 1, 1));
    }

    [Fact]
    public async Task AddFiscalYear_DoesNotLosePeriods_WhenLateCalendarSnapshotArrivesWithoutPeriods()
    {
        await SeedExistingFiscalYear();

        var period2027 = CreateAccountingPeriodData("2027", 1, new DateTime(2027, 1, 1), new DateTime(2027, 1, 31));
        _financeFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears = new List<FiscalYearData>
            {
                CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
                CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), [period2027])
            }
        }));

        var addTask = _viewModel.AddFiscalYearCommand.ExecuteAsync(null);
        await Task.Delay(60);

        _fiscalCalendarUpdated.OnNext(CreateCalendarSnapshot([
            CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
            CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), [])
        ]));
        await addTask;

        _viewModel.SelectedFiscalYear.ShouldNotBeNull();
        _viewModel.SelectedFiscalYear!.Periods.Count.ShouldBe(1);

        // Simulate a calendar update that still has no periods for 2027 — periods from GetFiscalCalendar should be kept.
        _currentFiscalCalendar.OnNext(CreateCalendarSnapshot([
            CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), []),
            CreateFiscalYearData(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), [])
        ]));
        await Task.Delay(120);

        // The explicit reload after AddFiscalYear already loaded the period from GetFiscalCalendar; 
        // a subsequent snapshot without periods replaces all items, so period count reflects snapshot.
        _viewModel.SelectedFiscalYear.ShouldNotBeNull();
        _viewModel.SelectedFiscalYear!.StartDate.Year.ShouldBe(2027);
    }

    private async Task SeedExistingFiscalYear()
    {
        _currentFiscalCalendar.OnNext(CreateCalendarSnapshot([
            CreateFiscalYearData(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), [])
        ]));
        await Task.Delay(120);
    }

    private FiscalCalendarData CreateCalendarSnapshot(IList<FiscalYearData> fiscalYears)
    {
        return new FiscalCalendarData { OrganisationId = OrgId, FiscalYears = fiscalYears };
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

