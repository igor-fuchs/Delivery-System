using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for Redis connection and caching configuration.
/// Bound to the <c>Redis</c> configuration section.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Redis";

    /// <summary>Gets the connection string used to connect to the Redis server.</summary>
    [Required]
    public required string ConnectionString { get; init; }

    /// <summary>Gets the prefix applied to all Redis keys to avoid collisions.</summary>
    public string InstanceName { get; init; } = "DeliverySystem:";

    /// <summary>Gets the time-to-live in minutes for the product cache entries.</summary>
    [Range(1, int.MaxValue)]
    public int ProductCacheTtlMinutes { get; init; } = 10;
}
