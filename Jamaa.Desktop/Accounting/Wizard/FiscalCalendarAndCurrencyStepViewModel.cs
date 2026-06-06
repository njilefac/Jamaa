using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting.Wizard;

public partial class FiscalCalendarAndCurrencyStepViewModel : ObservableObject
{
    public FiscalCalendarAndCurrencyStepViewModel(
        FiscalCalendarAndPeriodsViewModel fiscalCalendarViewModel,
        AccountingCurrencyAndDateFormatsViewModel currencyViewModel)
    {
        FiscalCalendarViewModel = fiscalCalendarViewModel;
        CurrencyViewModel = currencyViewModel;
    }

    public FiscalCalendarAndPeriodsViewModel FiscalCalendarViewModel { get; }
    public AccountingCurrencyAndDateFormatsViewModel CurrencyViewModel { get; }
}
