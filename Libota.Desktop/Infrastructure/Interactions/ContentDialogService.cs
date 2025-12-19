using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Controls;

namespace Libota.Desktop.Infrastructure.Interactions;

public class ContentDialogService : IDialogService
{
    public async Task<DialogResponse<TResult>> ShowAsync<TViewModel, TResult>(TViewModel viewModel)
        where TViewModel : IResultProvider<TResult>
    {
        var dialog = DialogFactory.Create<TViewModel>();
        dialog.DataContext = viewModel;

        var owner = GetActiveWindow();
        var result = await dialog.ShowAsync(owner);

        return new DialogResponse<TResult>(
            Confirmed: result == ContentDialogResult.Primary,
            Result: ExtractResult<TResult>(viewModel)
        );
    }
    
    private static Window GetActiveWindow()
    {
        var appWindow =  (Avalonia.Application.Current?.ApplicationLifetime
                   as IClassicDesktopStyleApplicationLifetime)?
               .MainWindow
               ?? throw new InvalidOperationException("No MainWindow available");
        
        return appWindow;
    }

    private static TResult ExtractResult<TResult>(object vm)
    {
        if (vm is IResultProvider<TResult> provider)
            return provider.Result;

        return default!;
    }
}