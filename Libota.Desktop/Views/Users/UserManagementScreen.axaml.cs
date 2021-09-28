using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Users;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Users
{
    public partial class UserManagementScreen : ReactiveUserControl<UserManagementViewModel>
    {
        public UserManagementScreen()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<UserManagementViewModel>();
                Disposable.Create(() => { }).DisposeWith(disposables);
            });
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}