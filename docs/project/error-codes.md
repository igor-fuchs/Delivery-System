# Error Codes Reference

All endpoints return errors as structured JSON. Use the `errorCode` field as an i18n translation key on the front-end or mobile client.

---

## Error Response Format

### General errors (4xx / 5xx except 400 validation)

```json
{
  "message": "Human-readable error description.",
  "errorCode": "SNAKE_CASE_CODE"
}
```

### Validation errors (400)

```json
{
  "message": "Validation failed.",
  "errorCode": "VALIDATION_FAILED",
  "errors": {
    "email": [
      { "code": "EMAIL_REQUIRED", "message": "'Email' must not be empty." }
    ],
    "password": [
      { "code": "PASSWORD_TOO_SHORT", "message": "Password must be at least 8 characters." },
      { "code": "PASSWORD_MISSING_UPPERCASE", "message": "Password must contain at least one uppercase letter." }
    ]
  }
}
```

> The `errors` field is a dictionary where the key is the field name (camelCase) and the value is an array of errors. It only appears in responses with `errorCode: "VALIDATION_FAILED"`.

---

## Exception → HTTP Mapping

| Exception | HTTP | When it occurs |
|---|---|---|
| `ValidationException` | 400 | Invalid data in the request body |
| `AppUnauthorizedException` | 401 | Invalid credentials, expired token, CAPTCHA failed |
| `NotFoundException` | 404 | Resource not found |
| `ConflictException` | 409 | Resource already exists (e.g. duplicate email) |
| `ServiceUnavailableException` | 503 | External service unavailable (e.g. email provider) |
| Unhandled exception | 500 | Unexpected internal server error |
| Rate limit exceeded | 429 | Too many requests in a short period |
| Forbidden | 403 | Authenticated user lacks permission (e.g. not admin) |

---

## General Codes

These codes appear in the `errorCode` field of responses from any endpoint.

| Code | HTTP | Description |
|---|---|---|
| `VALIDATION_FAILED` | 400 | One or more validation rules failed |
| `INTERNAL_ERROR` | 500 | Unexpected internal server error |
| `USER_IDENTITY_MISSING` | 401 | The authenticated user's identity could not be determined from the token |

---

## Auth Codes

Returned by `/api/auth/*` endpoints.

| Code | HTTP | Description | Endpoint |
|---|---|---|---|
| `USER_ALREADY_EXISTS` | 409 | An account with the given email already exists | `POST /register` |
| `USER_NOT_FOUND` | 404 | No account found for the given email | `POST /login` |
| `INVALID_CREDENTIALS` | 401 | Incorrect password | `POST /login` |
| `INVALID_GOOGLE_TOKEN` | 401 | The Google ID token could not be validated | `POST /google` |
| `GOOGLE_EMAIL_NOT_VERIFIED` | 401 | The Google account's email has not been verified | `POST /google` |
| `GOOGLE_EMAIL_CLAIM_MISSING` | 401 | The Google ID token does not contain an email claim | `POST /google` |
| `CAPTCHA_FAILED` | 401 | CAPTCHA verification failed | `POST /register`, `POST /login`, `POST /forgot-password` |
| `GOOGLE_JWKS_UNAVAILABLE` | 503 | The Google JWKS endpoint was unreachable | `POST /google` |
| `IDENTITY_ERROR` | 500 | An ASP.NET Identity operation returned one or more errors | Various |
| `INVALID_RESET_TOKEN` | 401 | The password reset token is invalid or has expired | `POST /reset-password` |
| `EMAIL_DELIVERY_FAILED` | 503 | The password reset email could not be delivered | `POST /forgot-password` |

---

## Products Codes

Returned by `/api/products/*` endpoints.

| Code | HTTP | Description | Endpoint |
|---|---|---|---|
| `PRODUCT_NOT_FOUND` | 404 | The requested product was not found | `GET /{id}`, `PUT /{id}`, `DELETE /{id}` |

---

## Orders Codes

Returned by `/api/orders/*` endpoints.

| Code | HTTP | Description | Endpoint |
|---|---|---|---|
| `ORDER_NOT_FOUND` | 404 | The requested order was not found | `GET /{id}`, `PUT /{id}`, `DELETE /{id}` |
| `ORDER_ACCESS_DENIED` | 403 | The authenticated user does not have permission to access this order | `GET /{id}` |
| `PRODUCT_UNAVAILABLE` | 400 | A product in the order is currently out of stock | `POST /` |

---

## Idempotency Codes

The `Idempotency-Key` header is required on `POST /api/orders`.

| Code | HTTP | Description |
|---|---|---|
| `IDEMPOTENCY_KEY_MISSING` | 400 | The `Idempotency-Key` header is missing |
| `IDEMPOTENCY_KEY_TOO_LONG` | 400 | The `Idempotency-Key` header exceeds the maximum allowed length |

---

## Validation Field Codes

These codes appear inside the `errors` dictionary in `400 VALIDATION_FAILED` responses.

### Auth — Common fields

| Code | Field | Violated rule |
|---|---|---|
| `EMAIL_REQUIRED` | `email` | Required field |
| `EMAIL_INVALID_FORMAT` | `email` | Invalid email format |
| `EMAIL_TOO_LONG` | `email` | Maximum 254 characters |
| `PASSWORD_REQUIRED` | `password` | Required field |
| `PASSWORD_TOO_SHORT` | `password` | Minimum 8 characters |
| `PASSWORD_TOO_LONG` | `password` | Maximum 128 characters |
| `PASSWORD_MISSING_UPPERCASE` | `password` | Must contain at least one uppercase letter |
| `PASSWORD_MISSING_LOWERCASE` | `password` | Must contain at least one lowercase letter |
| `PASSWORD_MISSING_DIGIT` | `password` | Must contain at least one digit |
| `PASSWORD_MISSING_SPECIAL` | `password` | Must contain at least one special character (`@$!%*?&`) |
| `CAPTCHA_TOKEN_REQUIRED` | `captchaToken` | Required field |
| `CAPTCHA_TOKEN_TOO_LONG` | `captchaToken` | Maximum 2000 characters |
| `GOOGLE_TOKEN_REQUIRED` | `idToken` | Required field |

### Auth — Password Reset

| Code | Field | Violated rule |
|---|---|---|
| `CALLBACK_URL_REQUIRED` | `callbackUrl` | Required field |
| `CALLBACK_URL_TOO_LONG` | `callbackUrl` | Maximum 2000 characters |
| `CALLBACK_URL_INVALID_FORMAT` | `callbackUrl` | Must be a valid absolute http/https URI |
| `USER_ID_REQUIRED` | `userId` | Required field |
| `USER_ID_TOO_LONG` | `userId` | Maximum 128 characters |
| `RESET_TOKEN_REQUIRED` | `token` | Required field |
| `RESET_TOKEN_TOO_LONG` | `token` | Maximum 2000 characters |
| `NEW_PASSWORD_REQUIRED` | `newPassword` | Required field |
| `NEW_PASSWORD_TOO_SHORT` | `newPassword` | Minimum 8 characters |
| `NEW_PASSWORD_TOO_LONG` | `newPassword` | Maximum 128 characters |
| `NEW_PASSWORD_MISSING_UPPERCASE` | `newPassword` | Must contain at least one uppercase letter |
| `NEW_PASSWORD_MISSING_LOWERCASE` | `newPassword` | Must contain at least one lowercase letter |
| `NEW_PASSWORD_MISSING_DIGIT` | `newPassword` | Must contain at least one digit |
| `NEW_PASSWORD_MISSING_SPECIAL` | `newPassword` | Must contain at least one special character (`@$!%*?&`) |

### Products

| Code | Field | Violated rule |
|---|---|---|
| `PRODUCT_NAME_REQUIRED` | `name` | Required field |
| `PRODUCT_NAME_TOO_LONG` | `name` | Maximum 200 characters |
| `PRODUCT_DESCRIPTION_REQUIRED` | `description` | Required field |
| `PRODUCT_DESCRIPTION_TOO_LONG` | `description` | Maximum 2000 characters |
| `PRODUCT_PRICE_TOO_LOW` | `price` | Must be greater than zero |
| `PRODUCT_PRICE_TOO_HIGH` | `price` | Maximum 10,000,000 |

### Orders

| Code | Field | Violated rule |
|---|---|---|
| `ORDER_DESCRIPTION_REQUIRED` | `description` | Required field |
| `ORDER_DESCRIPTION_TOO_LONG` | `description` | Maximum 2000 characters |
| `ORDER_ITEMS_REQUIRED` | `items` | Order must contain at least one item |
| `ORDER_ITEM_PRODUCT_ID_REQUIRED` | `items[n].productId` | Required field |
| `ORDER_ITEM_QUANTITY_TOO_LOW` | `items[n].quantity` | Must be greater than zero |
| `ORDER_ITEM_QUANTITY_TOO_HIGH` | `items[n].quantity` | Maximum 1000 units per item |
| `ORDER_STATUS_REQUIRED` | `status` | Required field |
| `ORDER_STATUS_INVALID` | `status` | Invalid value for the `OrderStatus` enum |

---

## OrderStatus Enum

Valid values for the `status` field in `PUT /api/orders/{id}`:

| Value | Description |
|---|---|
| `Pending` | Order placed, awaiting processing |
| `Processing` | Order is being prepared |
| `Shipped` | Order dispatched for delivery |
| `Delivered` | Order successfully delivered to recipient |
| `Cancelled` | Order was cancelled |
