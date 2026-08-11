using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spendnest.Infrastructure.Persistence;

public sealed class SpendnestDbContextFactory : IDesignTimeDbContextFactory<SpendnestDbContext>
{
    public SpendnestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SpendnestDbContext>();
        optionsBuilder.UseSqlite(SpendnestDataPaths.GetDefaultConnectionString());

        return new SpendnestDbContext(optionsBuilder.Options);
    }
}
