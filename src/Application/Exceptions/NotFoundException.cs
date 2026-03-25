namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested resource does not exist.
/// Maps to HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">A description of the missing resource.</param>
    public NotFoundException(string message) : base(message) { }
}
