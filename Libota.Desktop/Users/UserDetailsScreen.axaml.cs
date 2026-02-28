using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Users
{
    public partial class UserDetailsScreen : UserControl
    {
        public UserDetailsScreen()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}