using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members
{
    public class MemberRegistrationDialog : ReactiveWindow<MemberRegistrationViewModel>
    {
        public MemberRegistrationDialog()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<MemberRegistrationViewModel>();

                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}