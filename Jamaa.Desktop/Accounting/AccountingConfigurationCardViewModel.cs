using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Accounting;

public sealed class AccountingConfigurationCardViewModel : ObservableObject
{
    public AccountingConfigurationCardViewModel(string title, IRelayCommand? openCommand = null)
    {
        Title = title;
        OpenCommand = openCommand ?? new RelayCommand(static () => { });
    }

    public string Title { get; }

    public IRelayCommand OpenCommand { get; }
}