using DeliverySystem.Domain.Entities;
using DeliverySystem.Infrastructure.Identity;
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

    /// <summary>Gets the products table.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets the orders table.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Gets the order items table.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(2000);
            entity.Property(p => p.Stock).IsRequired();
            entity.Property(p => p.Price).IsRequired().HasColumnType("decimal(9,2)");
            entity.Property(p => p.CreatedAt).IsRequired();
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Description).IsRequired().HasMaxLength(2000);
            entity.Property(o => o.Status).IsRequired().HasConversion<string>();
            entity.Property(o => o.TotalAmount).IsRequired().HasColumnType("decimal(9,2)");
            entity.Property(o => o.CreatedAt).IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.Quantity).IsRequired();
            entity.Property(oi => oi.UnitPrice).IsRequired().HasColumnType("decimal(9,2)");

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
