using System.Linq;
using Domain.Values;
using EventFlow.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContextProvider : IDbContextProvider<LibotaDbContext>
    {
        private readonly IOptions<DatabaseOptions> _dbOptions;
        private readonly ILogger<LibotaDbContextProvider> _logger;

        public LibotaDbContextProvider(IOptions<DatabaseOptions> dbOptions, ILogger<LibotaDbContextProvider> logger)
        {
            _dbOptions = dbOptions;
            _logger = logger;
        }

        public LibotaDbContext CreateContext()
        {
            var context = new LibotaDbContext(_dbOptions);


            var database = context.Database;
            var pendingMigrations = database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any())
            {
                _logger.LogDebug($"found {pendingMigrations.Count} pending database migrations");
                database.MigrateAsync();
                _logger.LogDebug($"database schemas sucessfully updated");
            }
            return context;
        }
    }
}