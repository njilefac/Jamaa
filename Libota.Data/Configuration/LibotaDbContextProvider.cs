using Domain.Values;
using EventFlow.EntityFramework;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContextProvider : IDbContextProvider<LibotaDbContext>
    {
        private readonly IOptions<DatabaseOptions> _dbOptions;

        public LibotaDbContextProvider(IOptions<DatabaseOptions> dbOptions)
        {
            _dbOptions = dbOptions;
        }
        public LibotaDbContext CreateContext()
        {
            var context = new LibotaDbContext(_dbOptions);
            return context;
        }
    }
}