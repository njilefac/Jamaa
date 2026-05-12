using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Notifications;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Jamaa.Desktop.Accounting;

public partial class OpeningBalanceItemViewModel(
    IAccountingFacade accountingFacade,
    IUserSessionService userSessionService,
    INotificationService notificationService)
    : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _typeDisplay = string.Empty;
    [ObservableProperty] private bool _isLeafAccount;
    [ObservableProperty] private string _currencySymbol = string.Empty;
    [ObservableProperty] private int _decimalPrecision = 2;
    [ObservableProperty] private decimal _openingBalance;
    [ObservableProperty] private bool _isSaving;

    public void ForceFormatOpeningBalance()
    {
        OnPropertyChanged(nameof(OpeningBalance));
    }

    [ObservableProperty] private string _fiscalYearId = string.Empty;
    [ObservableProperty] private string _accountingPeriodId = string.Empty;

    [RelayCommand]
    private async Task SaveOpeningBalanceAsync()
    {
        var session = userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        var orgId = session.Organisation.Id;
        var subject = $"{Code} - {Name}";

        await notificationService.TrackOperationAsync(
            () => accountingFacade.SetAccountOpeningBalance(
                orgId,
                Id,
                FiscalYearId,
                AccountingPeriodId,
                OpeningBalance),
            BuildSaveConfirmationObservable(orgId),
            TimeSpan.FromSeconds(10),
            "Opening Balance",
            "Saved",
            subject,
            inFlight => IsSaving = inFlight);
    }

    private IObservable<bool> BuildSaveConfirmationObservable(string organisationId)
    {
        var events = accountingFacade.AccountOpeningBalanceSet
            .Where(x => x.AccountId == Id && x.FiscalYearId == FiscalYearId && x.AccountingPeriodId == AccountingPeriodId)
            .Select(_ => true);

        var readModelPolling = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .StartWith(0L)
            .SelectMany(_ => Observable.FromAsync(() =>
                HasOpeningBalanceAppearedAsync(organisationId)))
            .Where(isPresent => isPresent)
            .Take(1);

        return events.Merge(readModelPolling).Take(1);
    }

    private async Task<bool> HasOpeningBalanceAppearedAsync(string organisationId)
    {
        try
        {
            var balance = await accountingFacade.GetAccountOpeningBalance(
                organisationId,
                Id,
                FiscalYearId,
                AccountingPeriodId);

            return balance == OpeningBalance;
        }
        catch
        {
            return false;
        }
    }
}
