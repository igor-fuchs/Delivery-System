using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Domain.Entities;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

public sealed class ProductServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new ProductService(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static Product MakeProduct(string name = "Widget", bool stock = true, decimal price = 9.99m) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "A test product.",
        Stock = stock,
        Price = price,
        CreatedAt = DateTime.UtcNow
    };

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        var product = MakeProduct();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _sut.ExistsAsync(product.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_MissingId_ReturnsFalse()
    {
        var result = await _sut.ExistsAsync(Guid.NewGuid());

        Assert.False(result);
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithProducts_ReturnsAllProductsOrderedByName()
    {
        _context.Products.AddRange(MakeProduct("Zebra"), MakeProduct("Apple"), MakeProduct("Mango"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Mango", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProductResponse()
    {
        var product = MakeProduct("Widget");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(product.Id);

        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Widget", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAndReturnsResponse()
    {
        var request = new CreateProductRequest("Gadget", "A cool gadget.", true, 49.99m);

        var result = await _sut.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Gadget", result.Name);
        Assert.Equal("A cool gadget.", result.Description);
        Assert.True(result.Stock);
        Assert.Equal(49.99m, result.Price);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        Assert.Equal(1, await _context.Products.CountAsync());
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesFieldsAndReturns()
    {
        var product = MakeProduct("OldName");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var request = new UpdateProductRequest("NewName", "New description.", false, 19.99m);

        var result = await _sut.UpdateAsync(product.Id, request);

        Assert.Equal("NewName", result.Name);
        Assert.Equal("New description.", result.Description);
        Assert.False(result.Stock);
        Assert.Equal(19.99m, result.Price);
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest("Name", "Desc.", true, 1m);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), request));
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesFromStore()
    {
        var product = MakeProduct();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(product.Id);

        Assert.Equal(0, await _context.Products.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_MissingId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    #endregion
}
