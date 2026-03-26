namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with the current state (e.g. duplicate resource).
/// Maps to HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Gets the machine-readable error code for i18n translation lookup.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">A description of the conflict.</param>
    /// <param name="code">Machine-readable error code (see <see cref="Constants.ErrorCodes"/>).</param>
    public ConflictException(string message, string code) : base(message)
    {
        Code = code;
    }
}
