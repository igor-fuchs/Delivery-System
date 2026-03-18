using Microsoft.AspNetCore.Identity;

namespace DeliverySystem.Infrastructure.Data;

/// <summary>
/// ASP.NET Core Identity user entity for the delivery system.
/// Extends <see cref="IdentityUser{TKey}"/> with domain-specific properties.
/// This is the primary user entity managed by Identity's <see cref="UserManager{TUser}"/>.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Gets or sets the full name of the user.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the user was created.</summary>
    public DateTime CreatedAt { get; set; }
}
