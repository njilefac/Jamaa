using System;
using System.Collections.Generic;

namespace Libota.Data.Notifiers
{
    public interface IDataChangeNotifier : IObserver<KeyValuePair<string, object?>>
    {
        IObservable<object> Insertions { get; }
        IObservable<object> Updates { get; }
        IObservable<object> Deletions { get; }
    }
}