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

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public OrderService(ApplicationDbContext context)
    {
        _context = context;
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
        if (requesterId.HasValue && id != requesterId.Value)
            throw new UnauthorizedAccessException("You are not authorized to view this order.");

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product);

        var order = await query.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

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
            Description = request.Description,
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
            ?? throw new NotFoundException($"Order '{id}' was not found.");

        order.Status = Enum.Parse<OrderStatus>(request.Status, ignoreCase: true);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return MapToResponse(order);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _context.Orders.FindAsync([id], ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

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
            throw new NotFoundException($"Product '{missingId}' was not found.");

        var unavailable = products.FirstOrDefault(p => !p.Stock);
        if (unavailable is not null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["items"] = [$"Product '{unavailable.Name}' is currently unavailable."]
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
