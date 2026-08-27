using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nexus.Developer.Infrastructure.Sql;

// Design-time factory for `dotnet ef migrations add/remove/update` -- mirrors
// Nexus.Products.Chat.Infrastructure's NexusChatDbContextFactory pattern exactly
// (LocalDB default, env-var override so CI/other machines never need a checked-in
// connection string).
public sealed class NexusDeveloperDbContextFactory : IDesignTimeDbContextFactory<NexusDeveloperDbContext>
{
    private const string DefaultConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=NexusDeveloper;Trusted_Connection=True;MultipleActiveResultSets=True";

    public NexusDeveloperDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__NexusDeveloper")
            ?? DefaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<NexusDeveloperDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());

        return new NexusDeveloperDbContext(optionsBuilder.Options);
    }
}
