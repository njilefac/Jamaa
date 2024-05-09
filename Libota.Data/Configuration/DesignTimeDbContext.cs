using System.IO;
using Domain.Shared.Values;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration;

public class DesignTimeDbContext : IDesignTimeDbContextFactory<LibotaDbContext>
{
    public LibotaDbContext CreateDbContext(string[] args)
    {
        var dbOptions = new DatabaseOptions { DataFile = Path.Combine(Directory.GetCurrentDirectory(), "libota.db") };

        return new LibotaDbContext(Options.Create(dbOptions),
            LoggerFactory.Create(b => { }));
    }
}