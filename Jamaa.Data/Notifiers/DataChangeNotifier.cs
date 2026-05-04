using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

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
            // Notifications are published through NotifyCommittedChanges after successful SaveChanges.
        }

        public void NotifyCommittedChanges(IEnumerable<(EntityState State, object Entity)> changes)
        {
            foreach (var (state, entity) in changes)
            {
                switch (state)
                {
                    case EntityState.Added:
                        _insertions.OnNext(entity);
                        break;
                    case EntityState.Modified:
                        _updates.OnNext(entity);
                        break;
                    case EntityState.Deleted:
                        _deletions.OnNext(entity);
                        break;
                }
            }
        }

        public IObservable<object> Insertions => _insertions;
        public IObservable<object> Updates => _updates;
        public IObservable<object> Deletions => _deletions;
    }
}