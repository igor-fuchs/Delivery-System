using System.Text.Json;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

public sealed class CachedProductServiceTests
{
    private readonly IProductService _inner = Substitute.For<IProductService>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IProductService _sut;

    public CachedProductServiceTests()
    {
        var redisOptions = Options.Create(new RedisOptions
        {
            ConnectionString = "localhost",
            InstanceName = "Test:",
            ProductCacheTtlMinutes = 10
        });

        _sut = new CachedProductService(_inner, _cache, redisOptions);
    }

    private static ProductResponse MakeProduct(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Widget", "A product.", true, 9.99m, DateTime.UtcNow);

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_CacheMiss_ShouldCallInnerAndPopulateCache()
    {
        var products = new List<ProductResponse> { MakeProduct() };
        _cache.GetAsync("products:all", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);
        _inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(products);

        var result = await _sut.GetAllAsync();

        Assert.Single(result);
        await _inner.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "products:all",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_CacheHit_ShouldReturnCachedWithoutCallingInner()
    {
        var products = new List<ProductResponse> { MakeProduct() };
        var serialized = JsonSerializer.SerializeToUtf8Bytes(products);
        _cache.GetAsync("products:all", Arg.Any<CancellationToken>())
            .Returns(serialized);

        var result = await _sut.GetAllAsync();

        Assert.Single(result);
        await _inner.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_CacheMiss_ShouldCallInnerAndPopulateCache()
    {
        var id = Guid.NewGuid();
        var product = MakeProduct(id);
        _cache.GetAsync($"products:{id}", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);
        _inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _sut.GetByIdAsync(id);

        Assert.Equal(id, result.Id);
        await _inner.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            $"products:{id}",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ShouldReturnCachedWithoutCallingInner()
    {
        var id = Guid.NewGuid();
        var product = MakeProduct(id);
        var serialized = JsonSerializer.SerializeToUtf8Bytes(product);
        _cache.GetAsync($"products:{id}", Arg.Any<CancellationToken>())
            .Returns(serialized);

        var result = await _sut.GetByIdAsync(id);

        Assert.Equal(id, result.Id);
        await _inner.DidNotReceive().GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ShouldDelegateAndInvalidateAllProductsCache()
    {
        var request = new CreateProductRequest("New Product", "Desc", true, 19.99m);
        var created = MakeProduct();
        _inner.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(created);

        var result = await _sut.CreateAsync(request);

        Assert.Equal(created, result);
        await _inner.Received(1).CreateAsync(request, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("products:all", Arg.Any<CancellationToken>());
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldDelegateAndInvalidateBothCacheKeys()
    {
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest("Updated", "Desc", true, 29.99m);
        var updated = MakeProduct(id);
        _inner.UpdateAsync(id, request, Arg.Any<CancellationToken>())
            .Returns(updated);

        var result = await _sut.UpdateAsync(id, request);

        Assert.Equal(updated, result);
        await _inner.Received(1).UpdateAsync(id, request, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("products:all", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"products:{id}", Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ShouldDelegateAndInvalidateBothCacheKeys()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteAsync(id);

        await _inner.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("products:all", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"products:{id}", Arg.Any<CancellationToken>());
    }

    #endregion
}
