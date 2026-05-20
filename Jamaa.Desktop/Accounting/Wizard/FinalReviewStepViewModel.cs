using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Jamaa.Desktop.Accounting.Wizard;

public partial class FinalReviewStepViewModel : ObservableObject
{
    [ObservableProperty] private bool _isInitialized;

    public FinalReviewStepViewModel()
    {
    }

    [RelayCommand]
    private async Task InitializeLedger()
    {
        // Integration: Call the facade to initialize the ledger
        await Task.Delay(1000); // Simulate
        IsInitialized = true;
    }
}
