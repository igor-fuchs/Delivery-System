namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Exception thrown when one or more input validation rules fail.
/// Maps to HTTP 400 Bad Request and carries per-field error details with i18n codes.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Gets the validation errors grouped by property name.</summary>
    public IReadOnlyDictionary<string, ValidationFieldError[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">
    /// A dictionary mapping property names to their validation errors,
    /// each carrying a machine-readable <see cref="ValidationFieldError.Code"/> for i18n
    /// and a human-readable <see cref="ValidationFieldError.Message"/>.
    /// </param>
    public ValidationException(IReadOnlyDictionary<string, ValidationFieldError[]> errors)
        : base("Validation failed.")
    {
        Errors = errors;
    }
}
