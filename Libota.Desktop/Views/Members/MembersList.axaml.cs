using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Domain.Organisation.Requests;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.Infrastructure.Interactions;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

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
            var dialog = this.FindControl<ContentDialog>("AddMemberDialog");

            var nullResponse = new DialogResponse<MemberRegistrationRequest>(Confirmed: false, Result: null!);

            if (dialog == null)
            {
                throw new InvalidOperationException("Could not find AddMemberDialog in MembersList view.");
            }

            dialog.DataContext = interaction.Input;
            var result = await dialog.ShowAsync();

            var output = result == ContentDialogResult.Primary
                ? new DialogResponse<MemberRegistrationRequest>(
                    Confirmed: true,
                    Result: (dialog.DataContext as IResultProvider<MemberRegistrationRequest>)!.Result
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