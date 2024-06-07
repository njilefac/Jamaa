using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Finances;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Finances;

[SingleInstanceView]
public partial class FinanceManagementScreen : ReactiveUserControl<FinanceManagementViewModel>
{
    public FinanceManagementScreen()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
        ViewModel = Locator.Current.GetService<FinanceManagementViewModel>();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}