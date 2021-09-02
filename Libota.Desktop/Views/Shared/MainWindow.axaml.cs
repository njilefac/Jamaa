using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.Views.Setup;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<MainWindowViewModel>();
                
                Disposable.Create(() => { }).DisposeWith(disposables);
            });

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            var contentPageContainer = this.FindControl<RoutedViewHost>("ContentPageContainer");
            if (contentPageContainer != null)
            {
                contentPageContainer.DefaultContent = Locator.Current.GetService<CreateSuperUserScreen>();
            }
        }
    }
}
