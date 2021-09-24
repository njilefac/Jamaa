using System;
using Domain.Values;
using EventFlow.EntityFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContextProvider : IDbContextProvider<LibotaDbContext>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<DatabaseOptions> _options;
        private readonly ILoggerFactory _loggerFactory;

        public LibotaDbContextProvider(
            IServiceProvider serviceProvider,
            IOptions<DatabaseOptions> options,
            ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _loggerFactory = loggerFactory;
        }

        public LibotaDbContext CreateContext()
        {
            return new LibotaDbContext(_serviceProvider, _options, _loggerFactory);
        }
    }
}