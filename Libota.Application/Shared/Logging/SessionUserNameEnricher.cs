using Serilog.Core;
using Serilog.Events;

namespace Libota.Application.Shared.Logging;

public class SessionUserNameEnricher : ILogEventEnricher
{
    private LogEventProperty? _cachedProperty;

    private const string PROPERTY_NAME = "UserName";

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
        var currentUserName = "Test User";
        return propertyFactory.CreateProperty(PROPERTY_NAME, currentUserName);
    }
}