using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Domain.Entities;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

public sealed class OrderServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new OrderService(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<Guid> SeedUserAsync()
    {
        var email = $"user-{Guid.NewGuid()}@test.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Product> SeedProductAsync(bool stock = true, decimal price = 10m)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "A product.",
            Stock = stock,
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_NullCustomerId_ReturnsAllOrders()
    {
        var userId1 = await SeedUserAsync();
        var userId2 = await SeedUserAsync();
        var product = await SeedProductAsync();

        var request = new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]);
        await _sut.CreateAsync(userId1, request);
        await _sut.CreateAsync(userId2, request);

        var result = await _sut.GetAllAsync(null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithCustomerId_ReturnsOnlyOwnOrders()
    {
        var userId1 = await SeedUserAsync();
        var userId2 = await SeedUserAsync();
        var product = await SeedProductAsync();

        var request = new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]);
        await _sut.CreateAsync(userId1, request);
        await _sut.CreateAsync(userId2, request);

        var result = await _sut.GetAllAsync(userId1);

        Assert.Single(result);
        Assert.All(result, o => Assert.Equal(userId1, o.CustomerId));
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsOrderWithItems()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync(price: 5m);

        var request = new CreateOrderRequest("Delivery note", [new CreateOrderItemRequest(product.Id, 3)]);
        var created = await _sut.CreateAsync(userId, request);

        var result = await _sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Single(result.Items);
        Assert.Equal(15m, result.TotalAmount);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_OwnerRequesterId_ReturnsOrder()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync();
        var created = await _sut.CreateAsync(userId,
            new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]));

        var result = await _sut.GetByIdAsync(created.Id, requesterId: userId);

        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_DifferentRequesterId_ThrowsUnauthorizedAccessException()
    {
        var userId = await SeedUserAsync();
        var otherUserId = await SeedUserAsync();
        var product = await SeedProductAsync();
        var created = await _sut.CreateAsync(userId,
            new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetByIdAsync(created.Id, requesterId: otherUserId));
    }

    [Fact]
    public async Task GetByIdAsync_NullRequesterId_SkipsOwnershipCheck()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync();
        var created = await _sut.CreateAsync(userId,
            new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]));

        var result = await _sut.GetByIdAsync(created.Id, requesterId: null);

        Assert.Equal(created.Id, result.Id);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesPendingOrderWithSnapshotPrice()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync(price: 20m);

        var request = new CreateOrderRequest("Test order", [new CreateOrderItemRequest(product.Id, 2)]);

        var result = await _sut.CreateAsync(userId, request);

        Assert.Equal(userId, result.CustomerId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(40m, result.TotalAmount);
        Assert.Single(result.Items);
        Assert.Equal(20m, result.Items[0].UnitPrice);
    }

    [Fact]
    public async Task CreateAsync_MissingProduct_ThrowsNotFoundException()
    {
        var userId = await SeedUserAsync();

        var request = new CreateOrderRequest("Desc", [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(userId, request));
    }

    [Fact]
    public async Task CreateAsync_UnavailableProduct_ThrowsValidationException()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync(stock: false);

        var request = new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(userId, request));
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_ValidStatus_UpdatesAndSetsUpdatedAt()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync();
        var created = await _sut.CreateAsync(userId, new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]));

        var result = await _sut.UpdateStatusAsync(created.Id, new UpdateOrderStatusRequest("Shipped"));

        Assert.Equal("Shipped", result.Status);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_MissingId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateStatusAsync(Guid.NewGuid(), new UpdateOrderStatusRequest("Shipped")));
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesOrder()
    {
        var userId = await SeedUserAsync();
        var product = await SeedProductAsync();
        var created = await _sut.CreateAsync(userId, new CreateOrderRequest("Desc", [new CreateOrderItemRequest(product.Id, 1)]));

        await _sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_MissingId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    #endregion
}
