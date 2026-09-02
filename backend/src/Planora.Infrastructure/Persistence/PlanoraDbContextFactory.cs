using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planora.Infrastructure.Persistence;

public sealed class PlanoraDbContextFactory : IDesignTimeDbContextFactory<PlanoraDbContext>
{
    public PlanoraDbContext CreateDbContext(string[] args)
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=planora;Username=postgres;Password=CHANGE_ME_POSTGRES_PASSWORD;SSL Mode=Prefer";
        var connectionString = PostgreSqlConnectionString.Normalize(configuredConnectionString);
        var options = new DbContextOptionsBuilder<PlanoraDbContext>()
            .UseNpgsql(connectionString, postgreSql =>
                postgreSql.MigrationsAssembly(typeof(PlanoraDbContext).Assembly.FullName))
            .Options;
        return new PlanoraDbContext(options);
    }
}
