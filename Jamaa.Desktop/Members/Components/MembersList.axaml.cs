using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using FluentAvalonia.UI.Controls;
using Jamaa.Desktop.Services.Interactions;

namespace Jamaa.Desktop.Members.Components;

public partial class MembersList : UserControl, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public MembersList()
    {
        InitializeComponent();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _disposables.Dispose();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not MemberListViewModel vm) return;

        _disposables.Clear();
        _disposables.Add(vm.AddMemberRegistration.RegisterHandler(async interaction =>
        {
            var owner = TopLevel.GetTopLevel(this);
            var confirmed = await ShowDialogAsync(
                owner,
                "Register New Member",
                new MemberRegistrationView { DataContext = interaction.Input },
                "Register Member",
                "Cancel");

            var output = confirmed
                ? new DialogResponse<MemberRegistrationRequest>(
                    true,
                    interaction.Input.Result
                )
                : new DialogResponse<MemberRegistrationRequest>(false, null!);

            interaction.SetOutput(output);
        }));

        _disposables.Add(vm.ConfirmEndRegistration.RegisterHandler(async interaction =>
        {
            var owner = TopLevel.GetTopLevel(this);
            var confirmed = await ShowDialogAsync(
                owner,
                "End Registration",
                new MemberEndRegistrationView { DataContext = interaction.Input },
                "End Registration",
                "Cancel");

            var output = confirmed
                ? new DialogResponse<RegistrationStatus>(true, interaction.Input.Result)
                : new DialogResponse<RegistrationStatus>(false, default);

            interaction.SetOutput(output);
        }));

        _disposables.Add(vm.FocusSearch.RegisterHandler(interaction =>
        {
            SearchTermTextBox?.Focus();
            interaction.SetOutput(Unit.Default);
            return Task.CompletedTask;
        }));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SearchTermTextBox?.Focus();
    }

    private static async Task<bool> ShowDialogAsync(
        TopLevel? owner,
        string title,
        Control content,
        string primaryButtonText,
        string secondaryButtonText)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            DefaultButton = FAContentDialogButton.Primary
        };

        if (content.DataContext is MemberRegistrationViewModel registrationVm)
        {
            void UpdatePrimaryButtonState()
            {
                dialog.IsPrimaryButtonEnabled = registrationVm.CanRegister;
            }

            PropertyChangedEventHandler propertyChangedHandler = (_, args) =>
            {
                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(MemberRegistrationViewModel.CanRegister))
                    UpdatePrimaryButtonState();
            };

            EventHandler<DataErrorsChangedEventArgs> errorsChangedHandler = (_, _) => UpdatePrimaryButtonState();
            registrationVm.PropertyChanged += propertyChangedHandler;
            registrationVm.ErrorsChanged += errorsChangedHandler;

            dialog.Closed += (_, _) =>
            {
                registrationVm.PropertyChanged -= propertyChangedHandler;
                registrationVm.ErrorsChanged -= errorsChangedHandler;
            };

            UpdatePrimaryButtonState();
            dialog.PrimaryButtonClick += (_, args) =>
            {
                if (!registrationVm.ValidateForSubmit())
                    args.Cancel = true;
            };
        }

        if (owner is null)
            return await dialog.ShowAsync() == FAContentDialogResult.Primary;

        return await dialog.ShowAsync(owner) == FAContentDialogResult.Primary;
    }
}
