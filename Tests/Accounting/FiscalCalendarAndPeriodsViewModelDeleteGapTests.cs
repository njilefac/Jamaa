using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Jamaa.Application.Finances;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Services.Notifications;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public sealed class FiscalCalendarAndPeriodsViewModelDeleteGapTests : IDisposable
{
    private const string OrgId = "org-fiscal-delete-gap-tests";

    private readonly IFinanceManagementFacade _financeFacade;
    private readonly INotificationService _notificationService;
    private readonly Subject<FiscalYearData> _currentFiscalYears;
    private readonly Subject<FiscalYearData> _fiscalYearUpdated;
    private readonly Subject<FiscalYearData> _fiscalYearDeleted;
    private readonly FiscalCalendarAndPeriodsViewModel _viewModel;

    public FiscalCalendarAndPeriodsViewModelDeleteGapTests()
    {
        _financeFacade = Substitute.For<IFinanceManagementFacade>();
        _notificationService = Substitute.For<INotificationService>();

        _currentFiscalYears = new Subject<FiscalYearData>();
        _fiscalYearUpdated = new Subject<FiscalYearData>();
        _fiscalYearDeleted = new Subject<FiscalYearData>();

        _financeFacade.CurrentFiscalYears.Returns(_currentFiscalYears.AsObservable());
        _financeFacade.FiscalYearUpdated.Returns(_fiscalYearUpdated.AsObservable());
        _financeFacade.FiscalYearDeleted.Returns(_fiscalYearDeleted.AsObservable());

        var organisation = new OrganisationData { Id = OrgId, Name = "Test Organisation" };
        var userSession = new UserSession(true, "admin", Guid.NewGuid(), organisation);
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(userSession);

        _viewModel = new FiscalCalendarAndPeriodsViewModel(_financeFacade, userSessionService, _notificationService);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _currentFiscalYears.Dispose();
        _fiscalYearUpdated.Dispose();
        _fiscalYearDeleted.Dispose();
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
            NotificationType.Warning,
            null,
            null,
            null);

        _financeFacade.ReceivedCalls()
            .Any(call => call.GetMethodInfo().Name == nameof(IFinanceManagementFacade.DeleteFiscalYear))
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
        _financeFacade.GetFiscalYears(OrgId).Returns(Task.FromResult<IList<FiscalYearData>>(new List<FiscalYearData>
        {
            CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
        }));

        await InvokePrivateDeleteFiscalYear();

        _notificationService.Received(1).Show(
            "Fiscal year",
            Arg.Is<string>(message => message.Contains("Deleted FY 2027 successfully.", StringComparison.OrdinalIgnoreCase)),
            NotificationType.Success,
            null,
            null,
            null);

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
        _financeFacade.GetFiscalYears(OrgId).Returns(
            Task.FromResult<IList<FiscalYearData>>(new List<FiscalYearData>
            {
                CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))
            }),
            Task.FromException<IList<FiscalYearData>>(new InvalidOperationException("refresh failed")));

        await InvokePrivateDeleteFiscalYear();

        _viewModel.FiscalYears.Count.ShouldBe(1);
        _viewModel.FiscalYears.Single().StartDate.Year.ShouldBe(2026);
        _viewModel.StatusMessage.ShouldContain("refresh is still catching up", Case.Insensitive);
    }

    [Fact]
    public async Task DeleteFiscalYear_DoesNotReappear_WhenStaleCurrentFiscalYearSnapshotArrives()
    {
        await SeedTwoFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2027);
        var deletedFiscalYearId = _viewModel.SelectedFiscalYear!.Id.ToString();

        _financeFacade.DeleteFiscalYear(OrgId, deletedFiscalYearId).Returns(Task.CompletedTask);

        var survivingFiscalYear = CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        _financeFacade.GetFiscalYears(OrgId).Returns(Task.FromResult<IList<FiscalYearData>>([survivingFiscalYear]));

        await InvokePrivateDeleteFiscalYear();

        var staleDeletedSnapshot = new FiscalYearData
        {
            Id = deletedFiscalYearId,
            OrganisationId = OrgId,
            StartDate = new DateTime(2027, 1, 1),
            EndDate = new DateTime(2027, 12, 31),
            IsLocked = false,
            Periods = []
        };

        _currentFiscalYears.OnNext(staleDeletedSnapshot);
        await Task.Delay(120);

        _viewModel.FiscalYears.Count.ShouldBe(1);
        _viewModel.FiscalYears.Single().StartDate.Year.ShouldBe(2026);
    }

    [Fact]
    public async Task DeleteFiscalYear_DoesNotReappear_WhenStaleFiscalYearUpdatedSnapshotArrives()
    {
        await SeedTwoFiscalYears();

        _viewModel.SelectedFiscalYear = _viewModel.FiscalYears.First(fiscalYear => fiscalYear.StartDate.Year == 2027);
        var deletedFiscalYearId = _viewModel.SelectedFiscalYear!.Id.ToString();

        _financeFacade.DeleteFiscalYear(OrgId, deletedFiscalYearId).Returns(Task.CompletedTask);

        var survivingFiscalYear = CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        _financeFacade.GetFiscalYears(OrgId).Returns(Task.FromResult<IList<FiscalYearData>>([survivingFiscalYear]));

        await InvokePrivateDeleteFiscalYear();

        var staleDeletedUpdate = new FiscalYearData
        {
            Id = deletedFiscalYearId,
            OrganisationId = OrgId,
            StartDate = new DateTime(2027, 1, 1),
            EndDate = new DateTime(2027, 12, 31),
            IsLocked = false,
            Periods = []
        };

        _fiscalYearUpdated.OnNext(staleDeletedUpdate);
        await Task.Delay(120);

        _viewModel.FiscalYears.Count.ShouldBe(1);
        _viewModel.FiscalYears.Single().StartDate.Year.ShouldBe(2026);
    }

    private async Task SeedContiguousFiscalYears()
    {
        _currentFiscalYears.OnNext(CreateFiscalYearData("2022", new DateTime(2022, 1, 1), new DateTime(2022, 12, 31)));
        _currentFiscalYears.OnNext(CreateFiscalYearData("2023", new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)));
        _currentFiscalYears.OnNext(CreateFiscalYearData("2024", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        // ObserveOn(SynchronizationContext) callbacks run asynchronously.
        await Task.Delay(120);
    }

    private async Task SeedTwoFiscalYears()
    {
        _currentFiscalYears.OnNext(CreateFiscalYearData("2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));
        _currentFiscalYears.OnNext(CreateFiscalYearData("2027", new DateTime(2027, 1, 1), new DateTime(2027, 12, 31)));

        await Task.Delay(120);
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








