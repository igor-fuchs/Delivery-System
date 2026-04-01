using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Entities;
using DeliverySystem.Domain.Enums;
using DeliverySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// EF Core implementation of <see cref="IOrderService"/>.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICleanerService _cleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="sanitizer">The HTML input sanitizer.</param>
    public OrderService(ApplicationDbContext context, ICleanerService sanitizer)
    {
        _context = context;
        _cleaner = sanitizer;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(Guid? customerId, CancellationToken ct = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

        return orders.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<OrderResponse> GetByIdAsync(Guid id, Guid? requesterId = null, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.", ErrorCodes.OrderNotFound);

        if (requesterId.HasValue && order.CustomerId != requesterId.Value)
            throw new AppUnauthorizedException("You are not authorized to view this order.", ErrorCodes.OrderAccessDenied);

        return MapToResponse(order);
    }

    /// <inheritdoc />
    public async Task<OrderResponse> CreateAsync(Guid customerId, CreateOrderRequest request, CancellationToken ct = default)
    {
        var products = await LoadAndValidateProductsAsync(request.Items, ct);

        var items = BuildOrderItems(request.Items, products);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Description = _cleaner.Sanitize(request.Description),
            Status = OrderStatus.Pending,
            TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity),
            CreatedAt = DateTime.UtcNow,
            OrderItems = items
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(order.Id, ct: ct);
    }

    /// <inheritdoc />
    public async Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.", ErrorCodes.OrderNotFound);

        order.Status = Enum.Parse<OrderStatus>(request.Status, ignoreCase: true);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return MapToResponse(order);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _context.Orders.FindAsync([id], ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.", ErrorCodes.OrderNotFound);

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, Product>> LoadAndValidateProductsAsync(
        IReadOnlyList<CreateOrderItemRequest> items, CancellationToken ct)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        var missingId = productIds.FirstOrDefault(id => products.All(p => p.Id != id));
        if (missingId != default)
            throw new NotFoundException($"Product '{missingId}' was not found.", ErrorCodes.ProductNotFound);

        var unavailable = products.FirstOrDefault(p => !p.Stock);
        if (unavailable is not null)
        {
            throw new ValidationException(new Dictionary<string, ValidationFieldError[]>
            {
                ["items"] = [new ValidationFieldError(ErrorCodes.ProductUnavailable, $"Product '{unavailable.Name}' is currently unavailable.")]
            });
        }

        return products.ToDictionary(p => p.Id);
    }

    private static List<OrderItem> BuildOrderItems(
        IReadOnlyList<CreateOrderItemRequest> items, Dictionary<Guid, Product> products)
    {
        return items.Select(i => new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = products[i.ProductId].Price
        }).ToList();
    }

    private static OrderResponse MapToResponse(Order o) => new(
        o.Id,
        o.CustomerId,
        o.Description,
        o.Status.ToString(),
        o.TotalAmount,
        o.CreatedAt,
        o.UpdatedAt,
        o.OrderItems.Select(oi => new OrderItemResponse(
            oi.Id,
            oi.ProductId,
            oi.Product.Name,
            oi.Quantity,
            oi.UnitPrice)).ToList());
}
