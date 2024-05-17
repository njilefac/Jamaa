using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
public partial class MainMenu : ReactiveUserControl<MainMenuViewModel>
{
    public MainMenu()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel = Locator.Current.GetService<MainMenuViewModel>();
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
            
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}