using Domain.Values;
using EventFlow.EntityFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContextProvider : IDbContextProvider<LibotaDbContext>
    {
        private readonly IOptions<DatabaseOptions> _dbOptions;
        private readonly ILoggerFactory _loggerFactory;

        public LibotaDbContextProvider(IOptions<DatabaseOptions> dbOptions, ILoggerFactory loggerFactory)
        {
            _dbOptions = dbOptions;
            _loggerFactory = loggerFactory;
        }

        public LibotaDbContext CreateContext()
        {
            var context = new LibotaDbContext(_dbOptions, _loggerFactory);
            return context;
        }
    }
}