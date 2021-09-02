using System.IO;
using Domain.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class DesignTimeDBContextFactory : IDesignTimeDbContextFactory<LibotaDbContext>
    {
        public LibotaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LibotaDbContext>();
            var dbOptions = new DatabaseOptions{ DataFile = "libota.db"};
            optionsBuilder.UseSqlite($"Filename={dbOptions.DataFile}");

            
            return new LibotaDbContext(new OptionsWrapper<DatabaseOptions>(dbOptions));
        }
    }
}