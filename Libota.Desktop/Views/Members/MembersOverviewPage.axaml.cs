using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Organisation.Requests;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members
{
    public partial class MembersOverviewPage : ReactiveUserControl<MembersOverviewPageViewModel>
    {
        public MembersOverviewPage()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<MembersOverviewPageViewModel>();
                ViewModel?.ShowRegistrationPrompt.RegisterHandler(async interaction =>
                {
                    var dialog = Locator.Current.GetService<MemberRegistrationDialog>();
                    var request = await dialog?.ShowDialog<MemberRegistrationRequest>(interaction.Input)!;
                    interaction.SetOutput(request!);
                });
                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}