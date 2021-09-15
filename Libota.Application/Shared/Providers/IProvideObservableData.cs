using System;

namespace Libota.Application.Shared.Providers
{
    public interface IProvideObservableData<out TData>
    {
        IObservable<TData> Stream { get; }
    }
}