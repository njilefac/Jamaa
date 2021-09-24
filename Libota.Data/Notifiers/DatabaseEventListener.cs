using System;
using System.Diagnostics;
using Libota.Application.Shared.Providers;

namespace Libota.Data.Notifiers
{
    public class DatabaseEventListener : IObserver<DiagnosticListener>
    {
        private readonly IDataChangeNotifier _dataChangeNotifier;

        public DatabaseEventListener(IDataChangeNotifier dataChangeNotifier)
        {
            _dataChangeNotifier = dataChangeNotifier;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name.Equals("Microsoft.EntityFrameworkCore"))
            {
                value.Subscribe(_dataChangeNotifier);
            }
        }
    }
}