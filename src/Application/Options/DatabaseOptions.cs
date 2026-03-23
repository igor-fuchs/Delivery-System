using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for database connection configuration.
/// Bound to the <c>Database</c> configuration section.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Database";

    /// <summary>Gets the connection string used to connect to the SQL Server database.</summary>
    [Required]
    public required string ConnectionString { get; init; }
}
