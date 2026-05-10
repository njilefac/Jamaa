using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
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

public sealed class FiscalCalendarAndPeriodsViewModelDeleteGapTests : IDisposable
{
    private const string OrgId = "org-fiscal-delete-gap-tests";
    private readonly Subject<FiscalCalendarData> _currentFiscalCalendar;
    private readonly Subject<FiscalCalendarData> _fiscalCalendarUpdated;

    private readonly IAccountingFacade _financeFacade;
    private readonly INotificationService _notificationService;
    private readonly FiscalCalendarAndPeriodsViewModel _viewModel;

    public FiscalCalendarAndPeriodsViewModelDeleteGapTests()
    {
        _financeFacade = Substitute.For<IAccountingFacade>();
        _notificationService = Substitute.For<INotificationService>();

        _currentFiscalCalendar = new Subject<FiscalCalendarData>();
        _fiscalCalendarUpdated = new Subject<FiscalCalendarData>();

        _financeFacade.CurrentFiscalCalendar.Returns(_currentFiscalCalendar.AsObservable());
        _financeFacade.FiscalCalendarUpdated.Returns(_fiscalCalendarUpdated.AsObservable());

        var organisation = new OrganisationData { Id = OrgId, Name = "Test Organisation" };
        var userSession = new UserSession(true, "admin", Guid.NewGuid(), organisation);
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(userSession);

        _viewModel = new FiscalCalendarAndPeriodsViewModel(_financeFacade, userSessionService, _notificationService);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _currentFiscalCalendar.Dispose();
        _fiscalCalendarUpdated.Dispose();
    }

    [Fact]
    public async Task DeleteFiscalYearCommand_IsDisabled_WhenDeletingSelectedYearCreatesGap()
    {
        await SeedContiguousFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2023);

        _viewModel.DeleteFiscalYearCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteFiscalYear_ShowsWarning_AndDoesNotDispatch_WhenDeletingSelectedYearCreatesGap()
    {
        await SeedContiguousFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2023);

        await InvokePrivateDeleteFiscalYear();

        _viewModel.StatusMessage.ShouldContain("would create a gap");
        _notificationService.Received(1).Show(
            "Cannot delete fiscal year",
            Arg.Is<string>(message => message.Contains("would create a gap", StringComparison.OrdinalIgnoreCase)),
            NotificationType.Warning);

        _financeFacade.ReceivedCalls()
            .Any(call => call.GetMethodInfo().Name == nameof(IAccountingFacade.DeleteFiscalYear))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteFiscalYearCommand_IsEnabled_WhenDeletingEdgeYearKeepsTimelineContiguous()
    {
        await SeedContiguousFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2024);

        _viewModel.DeleteFiscalYearCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteFiscalYear_Succeeds_WhenReadModelNoLongerContainsYear_EvenIfDeleteEventIsMissing()
    {
        await SeedTwoFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2027);
        var deletedFiscalYearId = _viewModel.SelectedFiscalYear!.Id.ToString();

        _financeFacade.DeleteFiscalYear(OrgId, deletedFiscalYearId).Returns(Task.CompletedTask);
        _financeFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears = [CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))]
        }));

        await InvokePrivateDeleteFiscalYear();

        _notificationService.Received(1).Show(
            "Fiscal year",
            Arg.Is<string>(message =>
                message.Contains("Deleted FY 2027 successfully.", StringComparison.OrdinalIgnoreCase)),
            NotificationType.Success);

        _viewModel.FiscalYears.Count.ShouldBe(1);
        _viewModel.SelectedFiscalYear?.StartDate.Year.ShouldBe(2026);
    }

    [Fact]
    public async Task DeleteFiscalYear_RemovesRowFromUi_WhenRefreshThrowsAfterDeleteConfirmation()
    {
        await SeedTwoFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2027);
        var deletedFiscalYearId = _viewModel.SelectedFiscalYear!.Id.ToString();

        _financeFacade.DeleteFiscalYear(OrgId, deletedFiscalYearId).Returns(Task.CompletedTask);
        // GetFiscalCalendar always throws — simulates the reload failing after confirmed deletion.
        _financeFacade.GetFiscalCalendar(OrgId)
            .Returns(Task.FromException<FiscalCalendarData>(new InvalidOperationException("refresh failed")));

        var survivingCalendar = new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears = [CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))]
        };

        var deleteTask = InvokePrivateDeleteFiscalYear();
        await Task.Delay(60);

        // Confirm deletion via the calendar update stream; also push to the current-calendar stream
        // so ApplyFiscalCalendarSnapshot removes the deleted year before the reload is attempted.
        _fiscalCalendarUpdated.OnNext(survivingCalendar);
        _currentFiscalCalendar.OnNext(survivingCalendar);

        await deleteTask;
        // Allow the ObserveOn(threadPool) subscription to process the current-calendar update.
        await Task.Delay(120);

        _viewModel.FiscalYears.Count.ShouldBe(1);
        _viewModel.FiscalYears.Single().StartDate.Year.ShouldBe(2026);
        _viewModel.StatusMessage.Contains("refresh is still catching up", StringComparison.OrdinalIgnoreCase)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteFiscalYear_RemovesDeletedYear_WhenCalendarUpdatedConfirmsDeletion()
    {
        await SeedTwoFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2027);
        var deletedFiscalYearId = _viewModel.SelectedFiscalYear!.Id.ToString();

        _financeFacade.DeleteFiscalYear(OrgId, deletedFiscalYearId).Returns(Task.CompletedTask);

        var survivingCalendar = new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears = [CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))]
        };
        _financeFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(survivingCalendar));

        var deleteTask = InvokePrivateDeleteFiscalYear();
        await Task.Delay(60);

        // Emit calendar without the deleted fiscal year to confirm deletion.
        _fiscalCalendarUpdated.OnNext(survivingCalendar);
        await deleteTask;

        _viewModel.FiscalYears.Count.ShouldBe(1);
        _viewModel.FiscalYears.Single().StartDate.Year.ShouldBe(2026);
    }

    private async Task SeedContiguousFiscalYears()
    {
        _currentFiscalCalendar.OnNext(new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears =
            [
                CreateFiscalYearData("2022", new DateTime(2022, 1, 1), new DateTime(2022, 12, 31)),
                CreateFiscalYearData("2023", new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
                CreateFiscalYearData("2024", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
            ]
        });

        await WaitForFiscalYearsAsync(2022, 2023, 2024);
    }

    private async Task SeedTwoFiscalYears()
    {
        _currentFiscalCalendar.OnNext(new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears =
            [
                CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)),
                CreateFiscalYearData("2027", new DateTime(2027, 1, 1), new DateTime(2027, 12, 31))
            ]
        });

        await WaitForFiscalYearsAsync(2026, 2027);
    }

    private async Task WaitForFiscalYearsAsync(params int[] expectedStartYears)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            var loadedYears = _viewModel.FiscalYears
                .Select(fiscalYear => fiscalYear.StartDate.Year)
                .ToHashSet();

            if (expectedStartYears.All(loadedYears.Contains))
                return;

            await Task.Delay(20);
        }

        var actual = string.Join(", ", _viewModel.FiscalYears.Select(fiscalYear => fiscalYear.StartDate.Year));
        throw new TimeoutException($"Timed out waiting for fiscal years: {string.Join(", ", expectedStartYears)}. Actual: {actual}");
    }

    private static FiscalYearData CreateFiscalYearData(string suffix, DateTime startDate, DateTime endDate)
    {
        return new FiscalYearData
        {
            Id = Guid.NewGuid().ToString(),
            OrganisationId = OrgId,
            StartDate = startDate,
            EndDate = endDate,
            IsLocked = false,
            Periods =
            [
                new AccountingPeriodData
                {
                    Id = $"period-{suffix}-01",
                    OrganisationId = OrgId,
                    FiscalYearId = suffix,
                    SequenceNumber = 1,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsLocked = false
                }
            ]
        };
    }

    private async Task InvokePrivateDeleteFiscalYear()
    {
        var deleteMethod = typeof(FiscalCalendarAndPeriodsViewModel)
            .GetMethod("DeleteFiscalYear", BindingFlags.Instance | BindingFlags.NonPublic);

        deleteMethod.ShouldNotBeNull();

        var result = deleteMethod.Invoke(_viewModel, null);
        result.ShouldNotBeNull();
        await (Task)result;
    }
}