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

    // ── Validation field codes — Auth ─────────────────────────────────────────

    /// <summary>Email field is required.</summary>
    public const string EmailRequired = "EMAIL_REQUIRED";

    /// <summary>Email address format is invalid.</summary>
    public const string EmailInvalidFormat = "EMAIL_INVALID_FORMAT";

    /// <summary>Email address exceeds the maximum allowed length.</summary>
    public const string EmailTooLong = "EMAIL_TOO_LONG";

    /// <summary>Password field is required.</summary>
    public const string PasswordRequired = "PASSWORD_REQUIRED";

    /// <summary>Password is shorter than the minimum required length.</summary>
    public const string PasswordTooShort = "PASSWORD_TOO_SHORT";

    /// <summary>Password exceeds the maximum allowed length.</summary>
    public const string PasswordTooLong = "PASSWORD_TOO_LONG";

    /// <summary>Password must contain at least one uppercase letter.</summary>
    public const string PasswordMissingUppercase = "PASSWORD_MISSING_UPPERCASE";

    /// <summary>Password must contain at least one lowercase letter.</summary>
    public const string PasswordMissingLowercase = "PASSWORD_MISSING_LOWERCASE";

    /// <summary>Password must contain at least one digit.</summary>
    public const string PasswordMissingDigit = "PASSWORD_MISSING_DIGIT";

    /// <summary>Password must contain at least one special character.</summary>
    public const string PasswordMissingSpecial = "PASSWORD_MISSING_SPECIAL";

    /// <summary>CAPTCHA token field is required.</summary>
    public const string CaptchaTokenRequired = "CAPTCHA_TOKEN_REQUIRED";

    /// <summary>CAPTCHA token exceeds the maximum allowed length.</summary>
    public const string CaptchaTokenTooLong = "CAPTCHA_TOKEN_TOO_LONG";

    /// <summary>Google ID token field is required.</summary>
    public const string GoogleTokenRequired = "GOOGLE_TOKEN_REQUIRED";

    // ── Validation field codes — Products ─────────────────────────────────────

    /// <summary>Product name is required.</summary>
    public const string ProductNameRequired = "PRODUCT_NAME_REQUIRED";

    /// <summary>Product name exceeds the maximum allowed length.</summary>
    public const string ProductNameTooLong = "PRODUCT_NAME_TOO_LONG";

    /// <summary>Product description is required.</summary>
    public const string ProductDescriptionRequired = "PRODUCT_DESCRIPTION_REQUIRED";

    /// <summary>Product description exceeds the maximum allowed length.</summary>
    public const string ProductDescriptionTooLong = "PRODUCT_DESCRIPTION_TOO_LONG";

    /// <summary>Product price must be greater than zero.</summary>
    public const string ProductPriceTooLow = "PRODUCT_PRICE_TOO_LOW";

    /// <summary>Product price exceeds the maximum allowed value.</summary>
    public const string ProductPriceTooHigh = "PRODUCT_PRICE_TOO_HIGH";

    // ── Validation field codes — Orders ───────────────────────────────────────

    /// <summary>Order description is required.</summary>
    public const string OrderDescriptionRequired = "ORDER_DESCRIPTION_REQUIRED";

    /// <summary>Order description exceeds the maximum allowed length.</summary>
    public const string OrderDescriptionTooLong = "ORDER_DESCRIPTION_TOO_LONG";

    /// <summary>Order must contain at least one item.</summary>
    public const string OrderItemsRequired = "ORDER_ITEMS_REQUIRED";

    /// <summary>Order item product ID is required.</summary>
    public const string OrderItemProductIdRequired = "ORDER_ITEM_PRODUCT_ID_REQUIRED";

    /// <summary>Order item quantity must be greater than zero.</summary>
    public const string OrderItemQuantityTooLow = "ORDER_ITEM_QUANTITY_TOO_LOW";

    /// <summary>Order item quantity exceeds the maximum allowed value.</summary>
    public const string OrderItemQuantityTooHigh = "ORDER_ITEM_QUANTITY_TOO_HIGH";

    /// <summary>Order status field is required.</summary>
    public const string OrderStatusRequired = "ORDER_STATUS_REQUIRED";

    /// <summary>The provided order status value is not valid.</summary>
    public const string OrderStatusInvalid = "ORDER_STATUS_INVALID";

    // ── Service-level validation ───────────────────────────────────────────────

    /// <summary>A product in the order is currently out of stock.</summary>
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";
}
