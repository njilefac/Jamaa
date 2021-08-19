using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Club.Station.Desktop.Views.Users
{
    public class AddUserScreen : UserControl
    {
        public AddUserScreen()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}