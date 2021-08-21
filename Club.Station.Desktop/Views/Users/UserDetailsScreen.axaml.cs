using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Club.Station.Desktop.Views.Users
{
    public class UserDetailsScreen : UserControl
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