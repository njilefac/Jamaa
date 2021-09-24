using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Libota.Application.Shared.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Libota.Data.Notifiers
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

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (value.Key != CoreEventId.StateChanged.Name) return;
            try
            {
                var eventData = (StateChangedEventData)value.Value;
                var changedEntity = eventData.EntityEntry.Entity;
                switch (eventData.NewState)
                {
                    case EntityState.Unchanged:
                    {
                        _insertions.OnNext(changedEntity);
                        break;
                    }
                    case EntityState.Modified:
                    {
                        _updates.OnNext(changedEntity);
                        break;
                    }
                    case EntityState.Deleted:
                    {
                        _deletions.OnNext(changedEntity);
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