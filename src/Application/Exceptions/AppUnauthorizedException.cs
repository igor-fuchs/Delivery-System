namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when an authenticated request is rejected due to insufficient permissions
/// or invalid credentials. Maps to HTTP 401 Unauthorized.
/// </summary>
public sealed class AppUnauthorizedException : Exception
{
    /// <summary>
    /// Gets the machine-readable error code for i18n translation lookup.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppUnauthorizedException"/> class.
    /// </summary>
    /// <param name="message">Human-readable description of the authorization failure.</param>
    /// <param name="code">Machine-readable error code (see <see cref="Constants.ErrorCodes"/>).</param>
    public AppUnauthorizedException(string message, string code) : base(message)
    {
        Code = code;
    }
}
