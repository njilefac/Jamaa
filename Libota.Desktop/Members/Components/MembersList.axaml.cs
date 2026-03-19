using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Domain.Organisation.Requests;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.Services.Interactions;

namespace Libota.Desktop.Members.Components;

public partial class MembersList : UserControl, IDisposable
{
    private IDisposable? _handler;

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

        _handler?.Dispose();
        _handler = null;
        _handler = vm.AddMemberRegistration.RegisterHandler(async interaction =>
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
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _handler?.Dispose();
        _handler = null;
    }
}