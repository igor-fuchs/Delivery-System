using System.Text.Json;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Decorator around <see cref="ProductService"/> that adds distributed caching
/// for read operations and cache invalidation on write operations.
/// </summary>
public sealed class CachedProductService : IProductService
{
    private const string AllProductsCacheKey = "products:all";
    private const string ProductByIdCacheKeyPrefix = "products:";

    private readonly IProductService _inner;
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _cacheOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedProductService"/> class.
    /// </summary>
    /// <param name="inner">The underlying product service that accesses the database.</param>
    /// <param name="cache">The distributed cache store.</param>
    /// <param name="redisOptions">Redis configuration options.</param>
    public CachedProductService(
        IProductService inner,
        IDistributedCache cache,
        IOptions<RedisOptions> redisOptions)
    {
        _inner = inner;
        _cache = cache;
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(redisOptions.Value.ProductCacheTtlMinutes)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetStringAsync(AllProductsCacheKey, ct);

        if (cached is not null)
            return JsonSerializer.Deserialize<List<ProductResponse>>(cached)!;

        var products = await _inner.GetAllAsync(ct);

        await _cache.SetStringAsync(
            AllProductsCacheKey,
            JsonSerializer.Serialize(products),
            _cacheOptions,
            ct);

        return products;
    }

    /// <inheritdoc />
    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = $"{ProductByIdCacheKeyPrefix}{id}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
            return JsonSerializer.Deserialize<ProductResponse>(cached)!;

        var product = await _inner.GetByIdAsync(id, ct);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(product),
            _cacheOptions,
            ct);

        return product;
    }

    /// <inheritdoc />
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var result = await _inner.CreateAsync(request, ct);
        await _cache.RemoveAsync(AllProductsCacheKey, ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var result = await _inner.UpdateAsync(id, request, ct);
        await InvalidateProductCacheAsync(id, ct);
        return result;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _inner.DeleteAsync(id, ct);
        await InvalidateProductCacheAsync(id, ct);
    }

    private async Task InvalidateProductCacheAsync(Guid id, CancellationToken ct)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(AllProductsCacheKey, ct),
            _cache.RemoveAsync($"{ProductByIdCacheKeyPrefix}{id}", ct));
    }
}
