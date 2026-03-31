namespace DeliverySystem.Application.Constants;

/// <summary>
/// Machine-readable error codes returned in all error responses.
/// Front-end consumers should use these codes as i18n translation keys.
/// </summary>
public static class ErrorCodes
{
    // ── General ───────────────────────────────────────────────────────────────

    /// <summary>One or more validation rules failed.</summary>
    public const string ValidationFailed = "VALIDATION_FAILED";

    /// <summary>An unexpected internal server error occurred.</summary>
    public const string InternalError = "INTERNAL_ERROR";

    /// <summary>The authenticated user's identity could not be determined from the token.</summary>
    public const string UserIdentityMissing = "USER_IDENTITY_MISSING";

    // ── Auth ──────────────────────────────────────────────────────────────────

    /// <summary>A user with the given email already exists.</summary>
    public const string UserAlreadyExists = "USER_ALREADY_EXISTS";

    /// <summary>No account was found for the given email.</summary>
    public const string UserNotFound = "USER_NOT_FOUND";

    /// <summary>The provided password is incorrect.</summary>
    public const string InvalidCredentials = "INVALID_CREDENTIALS";

    /// <summary>The Google ID token could not be validated.</summary>
    public const string InvalidGoogleToken = "INVALID_GOOGLE_TOKEN";

    /// <summary>The Google account's email address has not been verified.</summary>
    public const string GoogleEmailNotVerified = "GOOGLE_EMAIL_NOT_VERIFIED";

    /// <summary>The Google ID token does not contain an email claim.</summary>
    public const string GoogleEmailClaimMissing = "GOOGLE_EMAIL_CLAIM_MISSING";

    /// <summary>CAPTCHA verification failed.</summary>
    public const string CaptchaFailed = "CAPTCHA_FAILED";

    /// <summary>The Google JWKS endpoint was unreachable.</summary>
    public const string GoogleJwksUnavailable = "GOOGLE_JWKS_UNAVAILABLE";

    /// <summary>An ASP.NET Identity operation returned one or more errors.</summary>
    public const string IdentityError = "IDENTITY_ERROR";

    /// <summary>The password reset token is invalid or has expired.</summary>
    public const string InvalidResetToken = "INVALID_RESET_TOKEN";

    /// <summary>The password reset email could not be delivered.</summary>
    public const string EmailDeliveryFailed = "EMAIL_DELIVERY_FAILED";

    // ── Products ──────────────────────────────────────────────────────────────

    /// <summary>The requested product was not found.</summary>
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";

    // ── Orders ────────────────────────────────────────────────────────────────

    /// <summary>The requested order was not found.</summary>
    public const string OrderNotFound = "ORDER_NOT_FOUND";

    /// <summary>The authenticated user is not allowed to access this order.</summary>
    public const string OrderAccessDenied = "ORDER_ACCESS_DENIED";

    // ── Idempotency ─────────────────────────────────────────────────────────

    /// <summary>The Idempotency-Key header is missing from the request.</summary>
    public const string IdempotencyKeyMissing = "IDEMPOTENCY_KEY_MISSING";

    /// <summary>The Idempotency-Key header exceeds the maximum allowed length.</summary>
    public const string IdempotencyKeyTooLong = "IDEMPOTENCY_KEY_TOO_LONG";

    // ── Service-level validation ───────────────────────────────────────────────

    /// <summary>A product in the order is currently out of stock.</summary>
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";
}
