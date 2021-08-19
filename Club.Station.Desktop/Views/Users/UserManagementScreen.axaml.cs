using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.ViewModels.Users;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Users
{
    public partial class UserManagementScreen : ReactiveUserControl<UserManagementViewModel>
    {
        public UserManagementScreen()
        {
            InitializeComponent();
            this.WhenActivated(disposables => {});
            DataContext = Locator.Current.GetService<UserManagementViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}