using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberRegistrationDialog : ReactiveUserControl<MemberRegistrationDialogViewModel>
{
    public MemberRegistrationDialog()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel = Locator.Current.GetService<MemberRegistrationDialogViewModel>();
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        FirstNameField?.Focus();
    }
}