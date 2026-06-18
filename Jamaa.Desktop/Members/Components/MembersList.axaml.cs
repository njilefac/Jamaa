using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
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
            var owner = this.FindAncestorOfType<Window>();
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
            var owner = this.FindAncestorOfType<Window>();
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

    private static async Task<bool> ShowDialogAsync(
        Window? owner,
        string title,
        Control content,
        string primaryButtonText,
        string secondaryButtonText)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 720,
            Height = 600,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var result = false;
        var completion = new TaskCompletionSource<bool>();

        var primaryButton = new Button
        {
            Content = primaryButtonText,
            IsDefault = true
        };
        primaryButton.Click += (_, _) =>
        {
            result = true;
            completion.TrySetResult(true);
            dialog.Close();
        };

        var secondaryButton = new Button
        {
            Content = secondaryButtonText,
            IsCancel = true
        };
        secondaryButton.Click += (_, _) =>
        {
            result = false;
            completion.TrySetResult(false);
            dialog.Close();
        };

        dialog.Closed += (_, _) => completion.TrySetResult(result);

        var buttonRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        buttonRow.Children.Add(secondaryButton);
        buttonRow.Children.Add(primaryButton);

        var layout = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };
        layout.Children.Add(content);
        layout.Children.Add(buttonRow);

        dialog.Content = layout;

        if (owner is null)
            dialog.Show();
        else
            await dialog.ShowDialog(owner);

        return await completion.Task;
    }
}