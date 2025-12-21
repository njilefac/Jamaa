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
        _handler?.Dispose();
        _handler = null;
        if (DataContext is MemberListViewModel vm)
        {
            _handler = vm.AddMemberRegistration.RegisterHandler(async interaction =>
            {
                var dialog = this.FindControl<ContentDialog>("AddMemberDialog");
                dialog?.DataContext = interaction.Input;
                var result = await dialog?.ShowAsync();
                if(result != ContentDialogResult.Primary)
                {
                    interaction.SetOutput(new DialogResponse<MemberRegistrationRequest>(
                        Confirmed: false,
                        Result: null!
                    ));
                    return;
                }
                
                interaction.SetOutput(new DialogResponse<MemberRegistrationRequest>(
                    Confirmed: true,
                    Result: (dialog.DataContext as MemberRegistrationViewModel)!.Result
                ));
            });
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _handler?.Dispose();
        _handler = null;
    }
}