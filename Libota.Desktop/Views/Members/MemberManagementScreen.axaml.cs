using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members
{
    public class MemberManagementScreen : ReactiveUserControl<MemberManagementViewModel>
    {
        public MemberManagementScreen()
        {
            InitializeComponent();
            this.WhenActivated(disposables => { });
            DataContext = Locator.Current.GetService<MemberManagementViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}