using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.ViewModels.Groups;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Groups
{
    public class GroupManagementScreen : ReactiveUserControl<GroupManagementViewModel>
    {
        public GroupManagementScreen()
        {
            InitializeComponent();
            this.WhenActivated(disposables => { });
            DataContext = Locator.Current.GetService<GroupManagementViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}