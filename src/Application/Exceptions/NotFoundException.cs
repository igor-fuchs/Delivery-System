namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested resource does not exist.
/// Maps to HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Gets the machine-readable error code for i18n translation lookup.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">A description of the missing resource.</param>
    /// <param name="code">Machine-readable error code (see <see cref="Constants.ErrorCodes"/>).</param>
    public NotFoundException(string message, string code) : base(message)
    {
        Code = code;
    }
}
