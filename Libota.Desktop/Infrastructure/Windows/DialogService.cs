using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace Libota.Desktop.Infrastructure.Windows;

public class DialogService(ITopLevelProvider topLevelProvider) : IDialogService
{
    public async Task<TOutput?> ShowDialog<TOutput>(object? viewModel)
    {
        var topLevel = topLevelProvider.Current ?? throw new InvalidOperationException("No active window found.");
        

        var dialog = new ContentDialog { Content = viewModel };

        var result = await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var response = await dialog.ShowAsync(topLevel);
            return response == ContentDialogResult.Primary
                ? (TOutput?)dialog.Content
                : default;
        });

        return result;
    }
}