using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Members
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