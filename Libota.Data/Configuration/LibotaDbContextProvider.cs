using Domain.Values;
using EventFlow.EntityFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContextProvider : IDbContextProvider<LibotaDbContext>
    {
        private readonly IOptions<DatabaseOptions> _options;
        private readonly ILoggerFactory _loggerFactory;

        public LibotaDbContextProvider(
            IOptions<DatabaseOptions> options,
            ILoggerFactory loggerFactory)
        {
            _options = options;
            _loggerFactory = loggerFactory;
        }

        public LibotaDbContext CreateContext()
        {
            return new LibotaDbContext(_options, _loggerFactory);
        }
    }
}