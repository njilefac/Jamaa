using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Setup;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class CreateOrganisationScreen : ReactiveUserControl<CreateOrganisationViewModel>
{
    public CreateOrganisationScreen()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel = Locator.Current.GetService<CreateOrganisationViewModel>();
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var nameField = this.FindControl<TextBox>("OrganisationNameField");
        nameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }
}