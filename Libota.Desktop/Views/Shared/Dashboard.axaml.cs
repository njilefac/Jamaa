using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.ViewModels.Navigation;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
public partial class Dashboard : ReactiveUserControl<DashboardViewModel>
{
    public Dashboard()
    {
        InitializeComponent();
            
        this.WhenActivated(disposables =>
        {
            ViewModel = Locator.Current.GetService<DashboardViewModel>();
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavigationView_OnSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        var viewModelType = (e.SelectedItem as NavigationItemViewModel)?.ViewModelType;
        if(viewModelType == ViewModel?.GetType() || e.IsSettingsSelected)
            return;
        
        var viewLocator = Locator.Current.GetService<IViewLocator>();
        var viewType = viewLocator?.ResolveView(Locator.Current.GetService(viewModelType))?.GetType();
        if(viewType != null)
            this.FindControl<Frame>("ContentFrame")?.Navigate(viewType);
    }
}