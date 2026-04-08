using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Entities;
using DeliverySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// EF Core implementation of <see cref="IProductService"/>.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly ICleanerService _cleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="sanitizer">The HTML input sanitizer.</param>
    public ProductService(ApplicationDbContext context, ICleanerService sanitizer)
    {
        _context = context;
        _cleaner = sanitizer;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return products.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
            throw new NotFoundException($"Product '{id}' was not found.", ErrorCodes.ProductNotFound);

        return MapToResponse(product);
    }

    /// <inheritdoc />
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = _cleaner.Clean(request.Name),
            Description = _cleaner.Clean(request.Description),
            Stock = request.Stock,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);

        return MapToResponse(product);
    }

    /// <inheritdoc />
    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _context.Products.FindAsync([id], ct);

        if (product is null)
            throw new NotFoundException($"Product '{id}' was not found.", ErrorCodes.ProductNotFound);

        product.Name = _cleaner.Clean(request.Name);
        product.Description = _cleaner.Clean(request.Description);
        product.Stock = request.Stock;
        product.Price = request.Price;

        await _context.SaveChangesAsync(ct);

        return MapToResponse(product);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _context.Products.FindAsync([id], ct);

        if (product is null)
            throw new NotFoundException($"Product '{id}' was not found.", ErrorCodes.ProductNotFound);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);
    }

    private static ProductResponse MapToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Stock, p.Price, p.CreatedAt);
}
