namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when a required external service is unreachable or unavailable.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public sealed class ServiceUnavailableException : Exception
{
    /// <summary>
    /// Gets the machine-readable error code for i18n translation lookup.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">A description of the unavailable service.</param>
    /// <param name="code">Machine-readable error code (see <see cref="Constants.ErrorCodes"/>).</param>
    public ServiceUnavailableException(string message, string code) : base(message)
    {
        Code = code;
    }
}
