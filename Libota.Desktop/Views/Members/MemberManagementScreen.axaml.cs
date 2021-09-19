using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members
{
    public class MemberManagementScreen : ReactiveUserControl<MembersOverviewPageViewModel>
    {
        public MemberManagementScreen()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<MembersOverviewPageViewModel>();
                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}