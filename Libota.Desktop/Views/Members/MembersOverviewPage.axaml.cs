using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MembersOverviewPage : ReactiveUserControl<MembersOverviewPageViewModel>
{
    public MembersOverviewPage()
    {
        InitializeComponent();

        ViewModel = Locator.Current.GetService<MembersOverviewPageViewModel>();

            
        this.WhenActivated(disposables =>
        {
            var handler = ViewModel?.ShowRegistrationPrompt.RegisterHandler(async interaction =>
            {
                var newMemberViewModel = Locator.Current.GetService<MemberRegistrationDialogViewModel>()!;

                var dialog = new ContentDialog
                {
                    DataContext = newMemberViewModel,
                    Content = Locator.Current.GetService<IViewFor<MemberRegistrationDialogViewModel>>()
                };
                var result = await dialog.ShowAsync();
                //var request = await dialog..ShowDialog<MemberRegistrationRequest>(mainWindow)!;
                    
                interaction.SetOutput(null);
            });
            disposables.Add(handler!);
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}