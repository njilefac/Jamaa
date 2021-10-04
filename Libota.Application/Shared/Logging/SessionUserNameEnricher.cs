using Serilog.Core;
using Serilog.Events;

namespace Libota.Application.Shared.Logging
{
    public class SessionUserNameEnricher : ILogEventEnricher
    {
        private LogEventProperty? _cachedProperty;

        public const string PropertyName = "SessionUserName";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(GetLogEventProperty(propertyFactory));
        }

        private LogEventProperty GetLogEventProperty(ILogEventPropertyFactory propertyFactory)
        {
            return _cachedProperty ??= CreateProperty(propertyFactory);
        }

        private LogEventProperty CreateProperty(ILogEventPropertyFactory propertyFactory)
        {
            var currentUserName = "Test User";
            return propertyFactory.CreateProperty(PropertyName, currentUserName);
        }
    }
}