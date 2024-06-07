using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberManagementScreen : ReactiveUserControl<MembersManagementScreenViewModel>
{
    public MemberManagementScreen()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel = Locator.Current.GetService<MembersManagementScreenViewModel>();
            var pagesHost = this.FindControl<RoutedViewHost>("PagesHost");
            if (pagesHost != null) 
                pagesHost.Content = GetCurrentPage();
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private static object? GetCurrentPage()
    {
        return Locator.Current.GetService<IViewFor<MembersOverviewPageViewModel>>();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}