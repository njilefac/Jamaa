using System;
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
    private IDisposable? _registrationHandler;
    private IDisposable? _endRegistrationHandler;

    public MembersList()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not MemberListViewModel vm)
        {
            return;
        }

        _registrationHandler?.Dispose();
        _registrationHandler = null;
        _registrationHandler = vm.AddMemberRegistration.RegisterHandler(async interaction =>
        {
            var dialog = new ContentDialog
            {
                Title = "Register New Member",
                PrimaryButtonText = "Register Member",
                SecondaryButtonText = "Cancel",
                Content = new MemberRegistrationView
                {
                    DataContext = interaction.Input
                }
            };

            var nullResponse = new DialogResponse<MemberRegistrationRequest>(Confirmed: false, Result: null!);

            var owner = this.FindAncestorOfType<Window>();
            var result = await dialog.ShowAsync(owner);

            var output = result == ContentDialogResult.Primary
                ? new DialogResponse<MemberRegistrationRequest>(
                    Confirmed: true,
                    Result: interaction.Input.Result
                )
                : nullResponse;

            interaction.SetOutput(output);
        });

        _endRegistrationHandler?.Dispose();
        _endRegistrationHandler = null;
        _endRegistrationHandler = vm.ConfirmEndRegistration.RegisterHandler(async interaction =>
        {
            var dialog = new ContentDialog
            {
                Title = "End Registration",
                PrimaryButtonText = "End Registration",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                Content = new MemberEndRegistrationView
                {
                    DataContext = interaction.Input
                }
            };

            var owner = this.FindAncestorOfType<Window>();
            var result = await dialog.ShowAsync(owner);

            var output = result == ContentDialogResult.Primary
                ? new DialogResponse<RegistrationStatus>(Confirmed: true, Result: interaction.Input.Result)
                : new DialogResponse<RegistrationStatus>(Confirmed: false, Result: default);

            interaction.SetOutput(output);
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _registrationHandler?.Dispose();
        _registrationHandler = null;
        _endRegistrationHandler?.Dispose();
        _endRegistrationHandler = null;
    }
}