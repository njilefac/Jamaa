using System;
using Serilog;
using Serilog.Configuration;

namespace Libota.Application.Shared.Logging
{
    public static class LoggingExtensions
    {
        public static LoggerConfiguration WithSessionUserName(
            this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich == null)
                throw new ArgumentNullException(nameof(enrich));

            var enricher = new SessionUserNameEnricher();
            return enrich.With(enricher);
        }
    }
}