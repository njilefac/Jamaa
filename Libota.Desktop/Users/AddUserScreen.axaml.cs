using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Users
{
    public partial class AddUserScreen : UserControl
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