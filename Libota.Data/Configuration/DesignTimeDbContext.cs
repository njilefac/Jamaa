using Domain.Values;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class DesignTimeDbContext : IDesignTimeDbContextFactory<LibotaDbContext>
    {
        public LibotaDbContext CreateDbContext(string[] args)
        {
            var dbOptions = new DatabaseOptions { DataFile = "libota.db" };

            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            return new LibotaDbContext(serviceProvider, Options.Create(dbOptions),
                LoggerFactory.Create(b => { b.AddConsole(); }));
        }
    }
}