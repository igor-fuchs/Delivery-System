using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeliverySystem.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations) to create
/// an <see cref="ApplicationDbContext"/> without running the full application host.
/// Reads the connection string from the <c>DATABASE__CONNECTION_STRING</c> environment variable.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE__CONNECTION_STRING")
            ?? throw new InvalidOperationException("The DATABASE__CONNECTION_STRING environment variable is not set.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
