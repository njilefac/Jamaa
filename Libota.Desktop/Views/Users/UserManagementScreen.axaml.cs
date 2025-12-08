using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.ViewModels.Users;

namespace Libota.Desktop.Views.Users
{
    [UsedImplicitly]
    public partial class UserManagementScreen : UserControl, IViewFor<UserManagementViewModel>
    {
        public UserManagementScreen(UserManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public new UserManagementViewModel? DataContext { get; set; }
    }
}