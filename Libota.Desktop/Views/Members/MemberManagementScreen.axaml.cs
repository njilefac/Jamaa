using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members
{
    public partial class MemberManagementScreen : ReactiveUserControl<MembersManagementScreenViewModel>
    {
        public MemberManagementScreen()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                ViewModel = Locator.Current.GetService<MembersManagementScreenViewModel>();
                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}