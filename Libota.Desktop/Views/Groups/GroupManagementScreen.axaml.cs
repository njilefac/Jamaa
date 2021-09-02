using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Groups;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Groups
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