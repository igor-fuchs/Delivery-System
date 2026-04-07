# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release

# Run a single test project
dotnet test tests/UnitTests/UnitTests.csproj
dotnet test tests/IntegrationTests/IntegrationTests.csproj

# Run a specific test by name filter
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Run the API locally (requires .env variables exported)
dotnet run --project src/Presentation/Presentation.csproj

# Run with Docker (recommended — starts API + SQL Server)
docker compose up --build

# EF Core migrations
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj \
  --output-dir Data/Migrations

dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj

dotnet ef migrations remove \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj
```

## Environment Setup

Copy `.env.example` to `.env` and fill in the values. Required variables:

| Variable | Purpose |
|---|---|
| `JWT__SECRET_KEY` | HMAC-SHA256 signing key (min 32 chars) |
| `DATABASE__CONNECTION_STRING` | SQL Server connection string |
| `DATABASE__SA_PASSWORD` | SQL Server SA password |
| `DATABASE__MSSQL_PID` | SQL Server edition (e.g. `Developer`) |
| `ADMIN_SEED__EMAIL` | Admin account seeded at startup |
| `ADMIN_SEED__PASSWORD` | Admin account password |
| `RECAPTCHA__SECRET_KEY` | Google reCAPTCHA v3 secret (production only) |

`Jwt__SecretKey` is never stored in `appsettings.json`. In development it comes from `.env`; in CI/CD from GitHub Secret `JWT_SECRET_KEY`.

In Development, the app auto-runs EF migrations and seeds roles/admin on startup. In production, run migrations explicitly in CI/CD.

## Architecture

This is a .NET 10 Clean Architecture solution with four layers, enforced by project-level dependencies (outer layers depend inward only):

```
Domain          — entities, constants, domain exceptions (no external dependencies)
Application     — use-case interfaces, DTOs, validators, options, app exceptions
Infrastructure  — EF Core, ASP.NET Identity, JWT, reCAPTCHA, Google OAuth2 implementations
Presentation    — ASP.NET controllers, middleware, filters, Program.cs
```

**Key design decisions:**

- **ASP.NET Core Identity** (`ApplicationUser : IdentityUser<Guid>`) handles password hashing (PBKDF2), roles, and lockout. `AuthService` in Infrastructure depends on `UserManager<ApplicationUser>` directly.
- **JWT** is issued by `TokenService` (Infrastructure) using `IOptions<JwtOptions>` (Application). The `JwtOptions` class uses `[Required]` + `ValidateOnStart()` so missing config fails at startup, not on the first request.
- **FluentValidation** validators live in Application. `ValidationFilter` in Presentation resolves `IValidator<T>` from DI and throws `Application.Exceptions.ValidationException` before the action executes.
- **`ExceptionHandlingMiddleware`** in Presentation catches all exceptions and maps them to structured JSON: `ValidationException` → 400, `ConflictException` → 409, `UnauthorizedAccessException` → 401, `ServiceUnavailableException` → 503, others → 500. Controllers have no try/catch.
- **reCAPTCHA**: In Development, `FakeCaptchaService` always passes. In Production, `RecaptchaService` calls Google's API via a typed `HttpClient`.
- **Rate limiting** is configured via `RateLimitOptions` (Application) and registered in `RateLimiterExtensions` (Presentation).
- All options classes (`JwtOptions`, `DatabaseOptions`, `CorsOptions`, `GoogleOptions`, `AdminSeedOptions`, `RecaptchaOptions`) live in Application and follow the same pattern: `[Required]` annotations + `ValidateOnStart()`.

**Roles:** `AppRoles.Admin` (`"admin"`) and `AppRoles.User` (`"user"`), defined in Domain. The `DefaultPolicy` requires either role. New users are assigned `user`; admin is seeded via `DatabaseSeeder`.

## Tests

- **Unit tests** (`tests/UnitTests/`) — xUnit + NSubstitute. Cover validators, exceptions, middleware, controller, `AuthService`, `TokenService`, `RecaptchaService`.
- **Integration tests** (`tests/IntegrationTests/`) — xUnit + `WebApplicationFactory<Program>`. Use SQLite in-memory (replacing SQL Server), `FakeCaptchaService`, and elevated rate limits. The `DeliverySystemFactory` wires all test infrastructure overrides.
- Integration tests are collected under `IntegrationTestCollection` to share one `DeliverySystemFactory` instance across test classes.

## Documentation Requirements

All public classes and methods must include XML documentation comments (`<summary>`, `<param>`, `<returns>`, `<exception>`). All API endpoints must have OpenAPI/Swagger annotations with response types. Add inline comments only to explain *why*, not *what*.

When asked to commit changes:
1. Run `git diff --staged` and `git diff` to see all changes
2. Group changes by logical responsibility:
   - feat: new functionality
   - fix: bug fix
   - refactor: refactoring without behavior change
   - chore: configs, deps, build
   - docs: documentation
3. Use `git add -p` or `git add <file>` for selective staging
4. Make one commit per group using conventional commits
5. Never mix, for example feat + fix in the same commit
6. Never add Co-Authored-By lines to commit messages
