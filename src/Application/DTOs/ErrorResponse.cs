using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Standard error response returned by all API endpoints when an error occurs.
/// Contains a human-readable message, a machine-readable error code for i18n,
/// and optional field-level validation errors.
/// </summary>
/// <param name="Message">Human-readable error description.</param>
/// <param name="ErrorCode">Machine-readable error code (e.g. <c>"VALIDATION_FAILED"</c>) used as an i18n key.</param>
/// <param name="Errors">
/// Field-level validation errors, keyed by field name.
/// Only present when <c>errorCode</c> is <c>"VALIDATION_FAILED"</c>.
/// </param>
public sealed record ErrorResponse(
    string Message,
    string ErrorCode,
    IReadOnlyDictionary<string, ValidationFieldError[]>? Errors = null);
