using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Domain.Organisation.Requests;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Members;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MembersOverviewPage : UserControl, IDisposable
{
    private IDisposable? _registration;

    public MembersOverviewPage()
    {
        InitializeComponent();
    }

    private async Task<MemberRegistrationRequest> ShowMemberRegistrationDialog()
    {
        //TODO: Implement dialog
        return await Task.FromResult(new MemberRegistrationRequest());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextEndUpdate()
    {
        base.OnDataContextEndUpdate();
        var viewModel = DataContext as MembersOverviewPageViewModel;

        // Dispose previous registration (if any) to avoid leaks and duplicate handlers
        _registration?.Dispose();
        _registration = null;

        if (viewModel != null)
        {
            _registration = viewModel.ShowRegistrationPrompt.RegisterHandler(ctx =>
            {
                ShowMemberRegistrationDialog()
                    .ContinueWith(x => { ctx.SetOutput(x.Result); });
            });
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _registration?.Dispose();
        _registration = null;
    }
}