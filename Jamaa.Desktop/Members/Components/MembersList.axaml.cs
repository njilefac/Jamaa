using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
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

            var nullResponse = new DialogResponse<MemberRegistrationRequest>(false, null!);

            var owner = this.FindAncestorOfType<Window>();
            var result = await dialog.ShowAsync(owner);

            var output = result == ContentDialogResult.Primary
                ? new DialogResponse<MemberRegistrationRequest>(
                    true,
                    interaction.Input.Result
                )
                : nullResponse;

            interaction.SetOutput(output);
        }));

        _disposables.Add(vm.ConfirmEndRegistration.RegisterHandler(async interaction =>
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
}