using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Domain.Organisation.Requests;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.Infrastructure.Interactions;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

public partial class MembersList : UserControl, IDisposable
{
    private IDisposable? _handler;
    private ContentDialog? _dialog;

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
            _dialog ??= this.FindControl<ContentDialog>("AddMemberDialog");

            var nullResponse = new DialogResponse<MemberRegistrationRequest>(Confirmed: false, Result: null!);

            if (_dialog == null)
            {
                throw new InvalidOperationException("Could not find AddMemberDialog in MembersList view.");
            }

            _dialog.DataContext = interaction.Input;
            var owner = this.GetVisualRoot() as Window;
            var result = await _dialog.ShowAsync(owner);

            var output = result == ContentDialogResult.Primary
                ? new DialogResponse<MemberRegistrationRequest>(
                    Confirmed: true,
                    Result: (_dialog.DataContext as IResultProvider<MemberRegistrationRequest>)!.Result
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