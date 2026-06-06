using System;
using System.Diagnostics;

namespace Jamaa.Data.Notifiers;

public class DatabaseEventListener(IDataChangeNotifier dataChangeNotifier) : IObserver<DiagnosticListener>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name.Equals("Microsoft.EntityFrameworkCore")) value.Subscribe(dataChangeNotifier);
    }
}