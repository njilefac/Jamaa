using System.IO;
using Domain.Shared.Values;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Jamaa.Data.Configuration;

public class DesignTimeDbContext : IDesignTimeDbContextFactory<JamaaDbContext>
{
    public JamaaDbContext CreateDbContext(string[] args)
    {
        var dbOptions = new DatabaseOptions { DataFile = Path.Combine(Directory.GetCurrentDirectory(), "jamaa.db") };

        return new JamaaDbContext(Options.Create(dbOptions));
    }
}