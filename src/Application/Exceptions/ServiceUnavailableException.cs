namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when a required external service is unreachable or unavailable.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public sealed class ServiceUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">A description of the unavailable service.</param>
    public ServiceUnavailableException(string message) : base(message) { }
}
