using System;
using System.Collections.Generic;
using EventFlow.Logs;
using Microsoft.Extensions.Logging;
using LogLevel = EventFlow.Logs.LogLevel;

namespace Libota.Application.Configuration
{
    public class LibotaEventLog : Log
    {
        private readonly ILogger _logger;

        private static IDictionary<LogLevel, Microsoft.Extensions.Logging.LogLevel> LogLevelMap =
            new Dictionary<LogLevel, Microsoft.Extensions.Logging.LogLevel>
            {
                [LogLevel.Debug] = Microsoft.Extensions.Logging.LogLevel.Debug,
                [LogLevel.Error] = Microsoft.Extensions.Logging.LogLevel.Error,
                [LogLevel.Fatal] = Microsoft.Extensions.Logging.LogLevel.Critical,
                [LogLevel.Information] = Microsoft.Extensions.Logging.LogLevel.Information,
                [LogLevel.Verbose] = Microsoft.Extensions.Logging.LogLevel.Trace,
                [LogLevel.Warning] = Microsoft.Extensions.Logging.LogLevel.Warning,
            };

        public LibotaEventLog(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(LibotaEventLog));
        }
        public override void Write(LogLevel logLevel, string format, params object[] args)
        {
            _logger.Log(MapLogLevel(logLevel), format, args);
        }

        public override void Write(LogLevel logLevel, Exception exception, string format, params object[] args)
        {
            _logger.Log(MapLogLevel(logLevel), exception, format, args);
        }

        protected override bool IsVerboseEnabled => true;
        protected override bool IsInformationEnabled => true;
        protected override bool IsDebugEnabled => true;
        private Microsoft.Extensions.Logging.LogLevel MapLogLevel(LogLevel logLevel)
        {
            return LogLevelMap[logLevel];
        }
    }
}