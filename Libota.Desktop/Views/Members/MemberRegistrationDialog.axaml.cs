using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members;

public partial class MemberRegistrationDialog : ReactiveWindow<MemberRegistrationDialogViewModel>
{
    public MemberRegistrationDialog()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<MemberRegistrationDialogViewModel>();

        this.WhenActivated(disposables =>
        {
            ViewModel?.RegisterMember.Subscribe(Close);
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