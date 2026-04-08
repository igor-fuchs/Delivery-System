using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Configuration options for Grafana observability platform.
/// Binds from the "Grafana" configuration section.
/// Used for Docker Compose environment variables and dashboard provisioning.
/// </summary>
public sealed class GrafanaOptions
{
    /// <summary>
    /// Configuration section name for binding Grafana options.
    /// </summary>
    public const string SectionName = "Grafana";

    /// <summary>
    /// Grafana admin user name.
    /// </summary>
    [Required]
    public required string AdminUser { get; init; }

    /// <summary>
    /// Grafana admin user password.
    /// </summary>
    [Required]
    public required string AdminPassword { get; init; }
}
