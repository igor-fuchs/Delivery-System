namespace DeliverySystem.Domain.Constants;

/// <summary>
/// Defines the application role names used for authorization.
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// Role assigned to regular users (customers).
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// Role assigned to providers (administrators).
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// Authorization policy that allows both <see cref="User"/> and <see cref="Admin"/> roles.
    /// </summary>
    public const string DefaultPolicy = "DefaultPolicy";
}
