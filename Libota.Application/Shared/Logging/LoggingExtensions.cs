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
            ArgumentNullException.ThrowIfNull(enrich);

            return enrich.With(new SessionUserNameEnricher());
        }
    }
}