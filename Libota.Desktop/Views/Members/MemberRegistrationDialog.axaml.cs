using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Members;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Members
{
    public class MemberRegistrationDialog : ReactiveWindow<MemberRegistrationDialogViewModel>
    {
        public MemberRegistrationDialog()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<MemberRegistrationDialogViewModel>();
                ViewModel!.RegisterMember.Subscribe(Close);
                Disposable.Create(() => { }).DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var firstNameField = this.FindControl<TextBox>("FirstNameField");
            
            firstNameField?.Focus();
        }
    }
}