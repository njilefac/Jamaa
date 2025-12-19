using System.Threading.Tasks;

namespace Libota.Desktop.Infrastructure.Interactions;

public interface IDialogService
{
    Task<DialogResponse<TResult>> ShowAsync<TViewModel, TResult>(TViewModel viewModel) where TViewModel : IResultProvider<TResult>;
}