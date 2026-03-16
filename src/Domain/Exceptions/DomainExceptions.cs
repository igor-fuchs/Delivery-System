namespace DeliverySystem.Domain.Exceptions;

/// <summary>
/// Represents an error that occurs when a domain invariant is violated.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Gets the machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="code">An optional machine-readable error code (defaults to <c>DOMAIN_ERROR</c>).</param>
    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="innerException">The exception that caused this domain error.</param>
    /// <param name="code">An optional machine-readable error code (defaults to <c>DOMAIN_ERROR</c>).</param>
    public DomainException(string message, Exception innerException, string code = "DOMAIN_ERROR")
        : base(message, innerException)
    {
        Code = code;
    }
}