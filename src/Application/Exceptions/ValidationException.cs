namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when one or more input validation rules fail.
/// Maps to HTTP 400 Bad Request and carries per-field error details.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Gets the validation errors grouped by property name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">A dictionary mapping property names to their validation error messages.</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
