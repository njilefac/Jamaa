using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Domain.Organisation.Requests;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;

namespace Libota.Desktop.Views.Members
{
    public partial class MembersOverviewPage : ReactiveUserControl<MembersOverviewPageViewModel>
    {
        public MembersOverviewPage(MembersOverviewPageViewModel membersOverviewPageViewModel, MemberRegistrationDialog memberRegistrationDialog)
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = membersOverviewPageViewModel;
                ViewModel?.ShowRegistrationPrompt.RegisterHandler(async interaction =>
                {
                    if (interaction.Input != null)
                    {
                        var request = await memberRegistrationDialog.ShowDialog<MemberRegistrationRequest>(interaction.Input);
                        interaction.SetOutput(request);
                    }
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