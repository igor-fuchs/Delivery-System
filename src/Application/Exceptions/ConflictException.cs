namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with the current state (e.g. duplicate resource).
/// Maps to HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">A description of the conflict.</param>
    public ConflictException(string message) : base(message) { }
}
