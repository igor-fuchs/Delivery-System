using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliverySystem.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the delivery system.
/// Extends <see cref="IdentityDbContext{TUser, TRole, TKey}"/> to include ASP.NET Core Identity tables.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });
    }
}
