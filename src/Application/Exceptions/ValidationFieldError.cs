namespace DeliverySystem.Application.Exceptions;

/// <summary>
/// Represents a single field-level validation error, carrying both a machine-readable
/// code (used by front-end i18n) and a human-readable message for developers.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g. <c>"EMAIL_REQUIRED"</c>).</param>
/// <param name="Message">Human-readable description of the validation failure.</param>
public sealed record ValidationFieldError(string Code, string Message);
