using Serilog.Core;
using Serilog.Events;

namespace Jamaa.Application.Shared.Logging;

public class SessionUserNameEnricher : ILogEventEnricher
{
    private LogEventProperty? _cachedProperty;

    private const string PropertyName = "UserName";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(GetLogEventProperty(propertyFactory));
    }

    private LogEventProperty GetLogEventProperty(ILogEventPropertyFactory propertyFactory)
    {
        return _cachedProperty ??= CreateProperty(propertyFactory);
    }

    private static LogEventProperty CreateProperty(ILogEventPropertyFactory propertyFactory)
    {
        const string currentUserName = "Test User";
        return propertyFactory.CreateProperty(PropertyName, currentUserName);
    }
}