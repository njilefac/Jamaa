using System;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Services;

public class LibotaViewLocator : IViewLocator
{
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        var viewType = typeof(IViewFor<>).MakeGenericType(viewModel?.GetType() ?? throw new InvalidOperationException());
        return Locator.Current.GetService(viewType, contract) as IViewFor;
    }
}