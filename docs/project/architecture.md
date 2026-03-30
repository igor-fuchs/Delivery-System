# Architecture

Delivery System follows **Clean Architecture** in 4 layers with unidirectional dependencies always pointing inward. No inner layer knows about outer layers.

---

## Layers and Dependencies

```
┌─────────────────────────────────────────────┐
│              Presentation                    │  ← ASP.NET Controllers, Middleware, Program.cs
│         (depends on Application)             │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│              Infrastructure                  │  ← EF Core, Identity, JWT, reCAPTCHA, Google OAuth2
│         (depends on Application)             │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│              Application                     │  ← Interfaces, DTOs, Validators, Options, Exceptions
│           (depends on Domain)                │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│                Domain                        │  ← Entities, Enums, Constants, DomainException
│         (no external dependencies)           │
└─────────────────────────────────────────────┘
```

---

## Layer Responsibilities

### Domain
- Business entities (`ApplicationUser`, `Product`, `Order`, `OrderItem`)
- Enums (`OrderStatus`, `AppRoles`)
- Domain constants
- `DomainException` — for business invariant violations
- No external NuGet dependencies

### Application
- Service interfaces (`IAuthService`, `IProductService`, `IOrderService`, `ICaptchaService`)
- Request and response DTOs
- FluentValidation validators (per feature: Auth, Products, Orders)
- Options classes (`JwtOptions`, `GoogleOptions`, `DatabaseOptions`, etc.)
- Application exceptions (`ValidationException`, `ConflictException`, `NotFoundException`, `AppUnauthorizedException`, `ServiceUnavailableException`)
- Error code constants (`ErrorCodes`)

### Infrastructure
- `ApplicationDbContext` (EF Core) with entity mappings
- `AuthService` — implements `IAuthService` using `UserManager<ApplicationUser>`
- `TokenService` — generates JWTs using `IOptions<JwtOptions>`
- `RecaptchaService` — validates tokens via HTTP against the Google API
- `FakeCaptchaService` — always passes in Development/Test environments
- EF Core migrations in `Data/Migrations/`
- `DatabaseSeeder` — creates roles and the admin account on startup

### Presentation
- Controllers (`AuthController`, `ProductsController`, `OrdersController`)
- `ExceptionHandlingMiddleware` — catches exceptions and returns structured JSON
- `ValidationFilter` — resolves `IValidator<T>` from DI and throws `ValidationException` before the action executes
- DI configuration in `Program.cs` and extension methods (`RateLimiterExtensions`, etc.)
- `IdempotencyFilter` — validates the `Idempotency-Key` header on creation endpoints
- `SwaggerIdempotencyFilter` — documents the header in Swagger

---

## Request Flow

```
HTTP Client
    │
    ▼
Rate Limiting Middleware  (429 if limit exceeded)
    │
    ▼
JWT Authentication        (401 if token invalid/missing)
    │
    ▼
Authorization             (403 if insufficient role)
    │
    ▼
ValidationFilter          (400 if FluentValidation fails)
    │
    ▼
Controller Action
    │
    ▼
Service (Application Interface → Infrastructure Implementation)
    │
    ▼
Repository / DbContext
    │
    ▼
ExceptionHandlingMiddleware  (catches any unhandled exception)
    │
    ▼
JSON Response
```

---

## Options Pattern

All external configuration is encapsulated in Options classes in the Application layer:

| Class | Section | Environment Variable |
|---|---|---|
| `JwtOptions` | `Jwt` | `JWT__SECRET_KEY` |
| `DatabaseOptions` | `Database` | `DATABASE__CONNECTION_STRING` |
| `GoogleOptions` | `Google` | `GOOGLE__WEBCLIENTID` |
| `RecaptchaOptions` | `Recaptcha` | `RECAPTCHA__SECRET_KEY` |
| `AdminSeedOptions` | `AdminSeed` | `ADMIN_SEED__EMAIL` |
| `CorsOptions` | `Cors` | — |
| `RateLimitOptions` | `RateLimit` | — |

All classes use `[Required]` on mandatory properties and are registered with `ValidateOnStart()` — if a variable is missing the application fails immediately at startup instead of failing on the first request.

---

## Authentication and Authorization

### Roles

Defined in `AppRoles` (Domain):

| Role | Value | Assignment |
|---|---|---|
| `AppRoles.Admin` | `"admin"` | Seeded via `DatabaseSeeder` on startup |
| `AppRoles.User` | `"user"` | Assigned automatically on registration |

The `DefaultPolicy` requires the user to have at least one of the two roles. Admin-only endpoints use `[Authorize(Roles = AppRoles.Admin)]`.

### Local Login Flow

1. Client sends `POST /api/auth/login` with email, password, and CAPTCHA token
2. `RecaptchaService` validates the CAPTCHA
3. `UserManager.FindByEmailAsync` locates the user
4. `UserManager.CheckPasswordAsync` verifies the password (PBKDF2)
5. `TokenService.GenerateToken` issues a JWT signed with HMAC-SHA256
6. JWT returned to the client

### Google Login Flow

1. Client obtains a Google ID Token in the app (web or mobile)
2. Sends `POST /api/auth/google` with the token
3. `GoogleJsonWebSignature.ValidateAsync` validates the token against Google's JWKS using `WebClientId` as the audience
4. If email not verified → 401
5. If user does not exist → auto-registration with `EmailConfirmed = true` (no password)
6. JWT returned to the client

---

## Error Handling

`ExceptionHandlingMiddleware` is the single point of exception-to-HTTP conversion. Controllers have no `try/catch`.

| Exception | HTTP |
|---|---|
| `ValidationException` | 400 |
| `AppUnauthorizedException` | 401 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `ServiceUnavailableException` | 503 |
| Any other | 500 |

See [Error Codes Reference](error-codes.md) for the full list of codes.

---

## Testing Strategy

### Unit Tests (`tests/UnitTests/`)

- Framework: xUnit + NSubstitute (mocks)
- Cover: validators, exceptions, middleware, controllers, `AuthService`, `TokenService`, `RecaptchaService`
- No I/O or database dependencies

### Integration Tests (`tests/IntegrationTests/`)

- Framework: xUnit + `WebApplicationFactory<Program>`
- `DeliverySystemFactory` replaces SQL Server with SQLite in-memory and `RecaptchaService` with `FakeCaptchaService`
- Rate limits elevated to avoid interfering with tests
- All tests share a single `DeliverySystemFactory` instance via `IntegrationTestCollection`
- Cover: end-to-end flows for Auth, Products, and Orders endpoints
