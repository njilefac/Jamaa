using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Notifiers
{
    public interface IDataChangeNotifier : IObserver<KeyValuePair<string, object?>>
    {
        IObservable<object> Insertions { get; }
        IObservable<object> Updates { get; }
        IObservable<object> Deletions { get; }

        void NotifyCommittedChanges(IEnumerable<(EntityState State, object Entity)> changes);
    }
}