using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Setup;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class OrganisationContactDetailsScreen : ReactiveUserControl<OrganisationContactDetailsViewModel>
{
    public OrganisationContactDetailsScreen()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel = Locator.Current.GetService<OrganisationContactDetailsViewModel>();
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}