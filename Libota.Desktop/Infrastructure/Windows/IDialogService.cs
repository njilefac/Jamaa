using System.Threading.Tasks;

namespace Libota.Desktop.Infrastructure.Windows;

public interface IDialogService
{
    Task<TOutput?> ShowDialog<TOutput>(object? viewModel);
}