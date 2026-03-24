using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jamaa.Data.Notifiers
{
    public class DataChangeNotifier : IDataChangeNotifier
    {
        private readonly ReplaySubject<object> _insertions;
        private readonly ReplaySubject<object> _updates;
        private readonly ReplaySubject<object> _deletions;

        public DataChangeNotifier()
        {
            _insertions = new ReplaySubject<object>();
            _updates = new ReplaySubject<object>();
            _deletions = new ReplaySubject<object>();
        }
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key != CoreEventId.StateChanged.Name) return;
            try
            {
                var eventData = value.Value as StateChangedEventData;
                var changedEntity = eventData?.EntityEntry.Entity;
                switch (eventData?.NewState)
                {
                    case EntityState.Unchanged:
                    {
                        if (eventData.OldState == EntityState.Added)
                        {
                            _insertions.OnNext(changedEntity ?? throw new InvalidOperationException());
                        }
                        else if (eventData.OldState == EntityState.Modified)
                        {
                            _updates.OnNext(changedEntity ?? throw new InvalidOperationException());
                        }
                        break;
                    }
                    case EntityState.Deleted:
                    {
                        _deletions.OnNext(changedEntity ?? throw new InvalidOperationException());
                        break;
                    }
                    case EntityState.Detached:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public IObservable<object> Insertions => _insertions;
        public IObservable<object> Updates => _updates;
        public IObservable<object> Deletions => _deletions;
    }
}