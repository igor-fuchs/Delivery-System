# Engineering Decision Log

---

## Decision: User authentication with JWT (Login & Register)
Date: 2026-03-14
Status: Superseded by "ASP.NET Core Identity with EF Core + SQL Server" (2026-03-18)

### Context
- The system needed user registration and login endpoints as the foundation for all authenticated operations.
- Clean Architecture must be respected: Domain holds entities, Application holds use-cases/DTOs/interfaces, Infrastructure holds implementations, Presentation holds controllers and HTTP concerns.
- The JWT secret key must never be committed to source control; it will be provided via GitHub Secrets in CI/CD and environment variables locally.

### Decision
- Created `User` entity in **Domain** with a factory method `User.Create()` that encapsulates invariant validation.
- Created `AuthService` in **Application** to orchestrate register/login flows. It depends on `IUserRepository`, `ITokenService`, and `IPasswordHasher` — all defined as interfaces in Application.
- Created `TokenService` (JWT via HMAC-SHA256), `PasswordHasher` (BCrypt), and `InMemoryUserRepository` in **Infrastructure**.
- Created `AuthController` in **Presentation** with `POST /api/auth/register` and `POST /api/auth/login`.
- JWT configuration bound to a strongly-typed `JwtOptions` class in **Application** using the Options pattern with `ValidateDataAnnotations()` and `ValidateOnStart()`.
- `SecretKey` is not stored in `appsettings.json`; it comes from the `Jwt__SecretKey` environment variable (mapped from `JWT_SECRET_KEY` in GitHub Secrets or `.env`).

### Alternatives Considered
- **ASP.NET Identity**: Full-featured but heavyweight for the current scope. Would introduce EF Core dependency prematurely. Rejected for now; can be adopted later.
- **Storing key in appsettings.json**: Simpler but insecure. Rejected — keys come from environment variables only.
- **IConfiguration injection in TokenService**: Originally used, replaced with `IOptions<JwtOptions>` for type safety and testability.

### Consequences
- **Positive**: Clean separation — controller has no try/catch, service is testable, JWT config is validated at startup.
- **Negative**: `InMemoryUserRepository` loses data on restart; acceptable as a placeholder.
- **Risk**: `required` keyword on `JwtOptions` properties is compile-time only; config binder ignores it. Mitigated by `[Required]` data annotations + `ValidateOnStart()`.

### Validation Plan
- Build check: `dotnet build` passes with zero errors and zero warnings.
- Runtime check: `ValidateOnStart()` fails fast if `SecretKey` is missing at startup.
- Tests to add: Unit tests for `AuthService` (register duplicate, login invalid credentials), `TokenService` (valid token structure), `PasswordHasher` (hash/verify round-trip).
- Rollback: Revert the commits that added Domain/Application/Infrastructure/Presentation auth files.

### Implementation Notes
- Key files: `Domain/Entities/User.cs`, `Application/Services/AuthService.cs`, `Application/Options/JwtOptions.cs`, `Application/Interfaces/I*.cs`, `Application/DTOs/*.cs`, `Infrastructure/Services/TokenService.cs`, `Infrastructure/Services/PasswordHasher.cs`, `Infrastructure/Repositories/InMemoryUserRepository.cs`, `Infrastructure/DependencyInjection.cs`, `Presentation/Controllers/AuthController.cs`, `Presentation/Program.cs`.
- GitHub Secrets: Create `JWT_SECRET_KEY` in repository settings.

- [ ] Replace `InMemoryUserRepository` with EF Core implementation
- [ ] Add unit tests for `AuthService`, `TokenService`, `PasswordHasher`
- [ ] Add integration tests for Auth endpoints

---

## Decision: Exception handling middleware and FluentValidation pipeline
Date: 2026-03-14
Status: Accepted

### Context
- Controllers had inline `try/catch` blocks mapping exceptions to HTTP status codes, duplicating error-handling logic across every action.
- Request validation used `DataAnnotations` on record parameters, which is limited (no conditional or cross-field rules) and produces inconsistent error shapes.

### Decision
- Created `ExceptionHandlingMiddleware` in **Presentation** that catches all exceptions and maps them to structured JSON responses with appropriate HTTP status codes (`ValidationException` → 400, `ConflictException` → 409, `UnauthorizedAccessException` → 401, others → 500).
- Created typed exceptions (`ConflictException`, `ValidationException`) in **Application** so the Application layer can throw domain-meaningful errors without depending on HTTP concepts.
- Replaced `DataAnnotations` with **FluentValidation** validators (`RegisterRequestValidator`, `LoginRequestValidator`) in **Application**.
- Created `ValidationFilter` (an `IAsyncActionFilter`) in **Presentation** that resolves `IValidator<T>` from DI at runtime and throws `ValidationException` before the action executes.
- Simplified `AuthController` to have no try/catch — exceptions flow to the middleware.

### Alternatives Considered
- **DataAnnotations only**: Simpler but limited; no per-rule messages, hard to test, tight coupling to ASP.NET model binding. Rejected.
- **MediatR pipeline behavior**: Would require MediatR adoption. Overkill for current scope. Can be added later.
- **`IExceptionHandler` (.NET 8+)**: Framework-native, but less flexible for custom JSON shapes and `IReadOnlyDictionary<string, string[]>` error format. Middleware gives full control.

### Consequences
- **Positive**: Controllers are clean; validation rules are centralized and testable; error response shape is consistent across all endpoints.
- **Negative**: Reflection-based validator resolution in `ValidationFilter` (`MakeGenericType`) has a minor performance cost. Acceptable for API workloads.
- **Risk**: Unregistered validators silently skip validation. Mitigated by `AddValidatorsFromAssemblyContaining<>` scanning.

### Validation Plan
- Build check: `dotnet build` passes.
- Runtime check: POST to `/api/auth/register` with invalid data returns structured 400 response.
- Tests to add: Unit tests for `RegisterRequestValidator`, `LoginRequestValidator`, integration test for middleware error mapping.
- Rollback: Remove middleware registration from `Program.cs`, re-add try/catch to controllers.

### Implementation Notes
- Key files: `Application/Exceptions/ConflictException.cs`, `Application/Exceptions/ValidationException.cs`, `Application/Validators/RegisterRequestValidator.cs`, `Application/Validators/LoginRequestValidator.cs`, `Presentation/Middlewares/ExceptionHandlingMiddleware.cs`, `Presentation/Filters/ValidationFilter.cs`.
- Package added: `FluentValidation.DependencyInjectionExtensions` 12.1.1 (centrally managed).

- [ ] Add unit tests for validators
- [ ] Add integration test asserting error response JSON structure
- [ ] Consider adding `NotFoundException` for future entity lookups

---

## Decision: Strongly-typed JWT configuration via Options pattern
Date: 2026-03-14
Status: Accepted

### Context
- JWT configuration (`SecretKey`, `Issuer`, `Audience`, `ExpirationMinutes`) was read via `IConfiguration["Jwt:..."]` in multiple places (`TokenService` and `Program.cs`), leading to duplicated magic strings and no startup validation.
- A null `SecretKey` caused a runtime `ArgumentNullException` deep in the JWT middleware on the first request, producing a confusing stack trace.

### Decision
- Created `JwtOptions` in **Application** with `[Required]` data annotations on all properties.
- Registered via `AddOptions<JwtOptions>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart()` in `Program.cs` — the app fails immediately at startup with a clear error if any required setting is missing.
- `TokenService` in **Infrastructure** now injects `IOptions<JwtOptions>` instead of `IConfiguration`.
- `Program.cs` binds `JwtOptions` from config and uses the typed object to configure `JwtBearerOptions`.

### Alternatives Considered
- **`required` keyword only**: Compile-time constraint; config binder ignores it at runtime. Caused the original null crash. Rejected alone; `[Required]` + `ValidateOnStart()` needed.
- **Manual validation in `Program.cs`**: Works but doesn't protect `TokenService` if config changes at runtime. Options validation is more robust.

### Consequences
- **Positive**: Fail-fast at startup with clear `OptionsValidationException`; no magic strings; `TokenService` is easily testable with mock `IOptions<JwtOptions>`.
- **Negative**: None significant.
- **Risk**: `appsettings.json` must not contain `SecretKey` — verified by `.env.example` documentation.

### Validation Plan
- Runtime check: Start app without `Jwt__SecretKey` → `OptionsValidationException` at startup.
- Tests to add: Unit test for `TokenService` with known `JwtOptions`.

### Implementation Notes
- Key files: `Application/Options/JwtOptions.cs`, `Infrastructure/Services/TokenService.cs`, `Presentation/Program.cs`.
- `JwtOptions` lives in Application because it's referenced by both Infrastructure (`TokenService`) and Presentation (`Program.cs`).

- [ ] Add unit test for `TokenService` token generation and claim verification

---

## Decision: Docker development environment with hot reload
Date: 2026-03-16
Status: Accepted

### Context
- The project needed a containerized development environment with hot reload so code changes are reflected without manual rebuilds.
- Initial Docker builds failed due to two issues: (1) `Directory.Build.Props` (uppercase P) vs Linux's case-sensitive filesystem expecting `Directory.Build.props`, and (2) Windows-built NuGet cache (`artifacts/`) being mounted into the Linux container with incompatible binary data.

### Decision
- Renamed `Directory.Build.Props` → `Directory.Build.props` and `Directory.Packages.Props` → `Directory.Packages.props` via `git mv` for cross-platform compatibility.
- Created a development-focused `Dockerfile` using the full `sdk:10.0` image with `dotnet watch run` for hot reload.
- Created `docker-compose.yml` that mounts the workspace as a volume (`.:/workspace`) with an anonymous volume (`/workspace/artifacts`) to isolate the Linux build cache from the Windows host.
- Set `DOTNET_USE_POLLING_FILE_WATCHER=true` for reliable file change detection across Docker volume mounts.
- Added `nuget.config` to ensure `nuget.org` is available inside the container.
- Created `.env.example` as a template for `JWT_SECRET_KEY` and other env vars; `.env` is git-ignored.

### Alternatives Considered
- **Multi-stage production Dockerfile**: Created first but replaced with dev-focused approach for the current phase. Production Dockerfile can be re-added as `Dockerfile.prod` when needed.
- **`docker-compose.override.yml` for dev**: Adds complexity with two files. Single dev-focused compose is simpler for now.
- **Bind mount artifacts/**: Caused `InvalidDataException` due to Windows/Linux binary incompatibility. Fixed with anonymous volume exclusion.

### Consequences
- **Positive**: Edits in VS Code trigger automatic recompilation inside the container; consistent environment across team members.
- **Negative**: `dotnet watch` can be slower than native on Windows due to volume mount overhead. Mitigated by polling watcher.
- **Risk**: Anonymous volume for `artifacts/` means container rebuild requires a fresh restore. Acceptable for dev.

### Validation Plan
- Tested: `docker compose up --build` → API starts on `http://localhost:8080` → `POST /api/auth/register` returns JWT.
- Rollback: Delete `Dockerfile`, `docker-compose.yml`, `.devcontainer/`.

### Implementation Notes
- Key files: `Dockerfile`, `docker-compose.yml`, `.devcontainer/devcontainer.json`, `.dockerignore`, `.env.example`, `nuget.config`.
- File renames: `Directory.Build.Props` → `.props`, `Directory.Packages.Props` → `.props`.

- [ ] Create `Dockerfile.prod` for production multi-stage builds
- [ ] Add health check endpoint for container orchestration
- [ ] Configure CI pipeline to use GitHub Secrets for `JWT_SECRET_KEY`

---

## Decision: VS Code DevContainer configuration
Date: 2026-03-16
Status: Accepted

### Context
- Developers need a one-click setup that provides the correct SDK, extensions, and environment variables without manual configuration.

### Decision
- Created `.devcontainer/devcontainer.json` using the `docker-compose.yml` as the backing infrastructure (`dockerComposeFile` reference).
- Configured VS Code extensions: C# Dev Kit, C# (OmniSharp), Docker, REST Client, EditorConfig, GitLens, GitHub Copilot.
- Workspace folder set to `/workspace` matching the Docker volume mount.
- Port 8080 forwarded automatically.

### Alternatives Considered
- **Standalone Dockerfile in devcontainer**: Would duplicate the Docker setup. Using `dockerComposeFile` reuses the existing compose definition.
- **Codespaces-only**: Limits to GitHub Codespaces users. Local devcontainer is more accessible.

### Consequences
- **Positive**: `Ctrl+Shift+P → Reopen in Container` gives a fully configured environment; new contributors are productive immediately.
- **Negative**: Requires Docker Desktop installed locally.

### Validation Plan
- Manual test: Reopen in Container → IntelliSense works → `dotnet build` succeeds → REST Client can hit API.

### Implementation Notes
- Key file: `.devcontainer/devcontainer.json`.
- Extension: `humao.rest-client` pairs with `requests.http` for API testing.

- [ ] Add `postStartCommand` to auto-run the API on container open
- [ ] Test DevContainer in GitHub Codespaces

---

## Decision: HTTP test file for API validation
Date: 2026-03-16
Status: Accepted

### Context
- Needed a quick way to test all auth endpoints without external tools like Postman.

### Decision
- Created `requests.http` at the repository root with test cases for: successful register, validation error (weak password), conflict (duplicate email), successful login, invalid credentials, and authenticated request with token chaining via `@name login` + `{{login.response.body.token}}`.

### Alternatives Considered
- **Postman collection**: Requires external tool and JSON export. `.http` files are version-controlled and work natively with VS Code REST Client extension.
- **Swagger UI only**: Good for exploration but doesn't save repeatable test sequences.

### Consequences
- **Positive**: Version-controlled, zero-setup API testing; token captured from login response and reused automatically.
- **Negative**: REST Client extension required (included in DevContainer config).

### Implementation Notes
- Key file: `requests.http`.
- Base URL defaults to `http://localhost:8080`.

- [ ] Add more test cases as new endpoints are created
- [ ] Add environment switching (dev/staging) in REST Client settings

---

## Decision: ASP.NET Core Identity with EF Core + SQL Server
Date: 2026-03-18
Status: Accepted

### Context
- The initial implementation used a custom `User` entity with a factory method, `IPasswordHasher` (BCrypt wrapper), and `InMemoryUserRepository` (ConcurrentDictionary). Data was lost on every restart and the setup lacked account lockout, email confirmation, and role management.
- A production-grade persistence layer and identity system were needed before adding Google OAuth2 and other features.

### Decision
- Replaced the custom `User` entity with `ApplicationUser` extending `IdentityUser<Guid>` in **Infrastructure**, adding only a `CreatedAt` property. Moved user identity management entirely to ASP.NET Core Identity.
- Replaced `InMemoryUserRepository` and `IPasswordHasher` with `UserManager<ApplicationUser>` provided by Identity. `AuthService` in Infrastructure now depends on `UserManager` directly.
- Created `ApplicationDbContext` extending `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` in **Infrastructure** for EF Core persistence.
- Created `DatabaseOptions` in **Application** with `[Required]` validation on `ConnectionString`, bound to the `Database` section via the Options pattern.
- Registered `AddDbContext<ApplicationDbContext>` in `DependencyInjection.cs` using `IOptions<DatabaseOptions>`, and configured `AddIdentityCore<ApplicationUser>` with password complexity rules and `RequireUniqueEmail`.
- SQL Server container added to `docker-compose.yml` with a health check; auto-migration runs in Development via `dbContext.Database.Migrate()`.

### Alternatives Considered
- **Keep InMemoryUserRepository + add EF Core only**: Would still require manual password hashing, lockout, and role logic. Identity provides all of this out of the box.
- **PostgreSQL**: Viable, but SQL Server was chosen for the team's existing familiarity. Can be swapped by changing the provider and connection string.
- **Separate migration host**: Auto-migrate in `Program.cs` is acceptable for development. Production should use an explicit migration step in CI/CD.

### Consequences
- **Positive**: Production-ready persistence; Identity handles password hashing (PBKDF2), validation, lockout, and roles; `[x]` closes the `InMemoryUserRepository` follow-up from Decision #1.
- **Negative**: Heavier startup (EF Core + SQL Server container). Development requires Docker for the database or a local SQL Server instance.
- **Risk**: `Database.Migrate()` in code runs on every startup in Development. Must not be enabled in Production — guarded by `IsDevelopment()` check.

### Validation Plan
- Build check: `dotnet build` passes.
- Runtime check: `docker compose up` → SQL Server healthy → API auto-migrates → Identity tables created.
- Tests to add: Integration tests with `WebApplicationFactory` and in-memory SQL Server provider.
- Rollback: Revert Identity/EF Core registration in `DependencyInjection.cs`, restore `InMemoryUserRepository`.

### Implementation Notes
- Key files: `Infrastructure/Data/ApplicationUser.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, `Application/Options/DatabaseOptions.cs`, `Infrastructure/DependencyInjection.cs`, `Presentation/Program.cs`, `docker-compose.yml`.
- Supersedes: Custom `User` entity, `IPasswordHasher`, `PasswordHasher`, `InMemoryUserRepository`, `IUserRepository`.

- [x] Replace `InMemoryUserRepository` with EF Core implementation (from Decision #1)
- [ ] Add integration tests with `WebApplicationFactory`
- [ ] Configure explicit migration step for production CI/CD
- [ ] Add database seeding for default roles

---

## Decision: Google OAuth2 federated login
Date: 2026-03-18
Status: Accepted

### Context
- Users need to sign in with their Google accounts (web and mobile) without creating a local password.
- The API must validate the Google ID token server-side and auto-register the user on first login.

### Decision
- Added `GoogleLoginRequest(string IdToken)` DTO in **Application** and `GoogleLoginAsync` to `IAuthService`.
- Created `GoogleOptions` in **Application** with `[Required]` validation on `WebClientId` and `MobileClientId`, bound to the `Google` config section.
- `AuthService.GoogleLoginAsync` in **Infrastructure** validates the ID token via `GoogleJsonWebSignature.ValidateAsync` with both client IDs as valid audiences. Rejects tokens with unverified emails or missing email claims.
- Auto-registers the user with `EmailConfirmed = true` (Google already verified the address) and no password.
- Added `POST /api/auth/google` endpoint in `AuthController`.
- `Google.Apis.Auth` NuGet package added.

### Alternatives Considered
- **OAuth2 authorization code flow**: More complex server-side; requires redirect handling. ID token validation is simpler for mobile-first APIs.
- **Single client ID only**: Would reject tokens from the other platform. Accepting both web and mobile IDs covers all clients.

### Consequences
- **Positive**: Frictionless sign-up via Google; no password to manage for OAuth users; works for both web and mobile clients.
- **Negative**: Dependency on Google's public key infrastructure for token validation (outage = login failure for Google users).
- **Risk**: `MobileClientId` is currently empty in config — mobile login will fail until configured. Listed as explicit follow-up.

### Validation Plan
- Runtime check: POST `/api/auth/google` with a valid Google ID token → 200 + JWT.
- Tests to add: Unit test for `GoogleLoginAsync` with mocked `GoogleJsonWebSignature`.
- Rollback: Remove `GoogleLoginAsync` from `IAuthService` and `AuthService`, remove `/api/auth/google` endpoint.

### Implementation Notes
- Key files: `Application/DTOs/GoogleLoginRequest.cs`, `Application/Options/GoogleOptions.cs`, `Infrastructure/Services/AuthService.cs`, `Presentation/Controllers/AuthController.cs`.
- Config: `appsettings.json` → `Google:WebClientId`, `Google:MobileClientId`.

- [ ] Configure `MobileClientId` when mobile app is set up
- [ ] Add unit test for Google login flow (valid token, invalid token, unverified email)

---

## Decision: Google reCAPTCHA v3 bot protection
Date: 2026-03-18
Status: Accepted

### Context
- Auth endpoints (register, login) are exposed publicly and vulnerable to credential stuffing and automated registration bots.
- A server-side CAPTCHA check is needed before processing any auth request.

### Decision
- Created `ICaptchaService` interface in **Application** with `Task<bool> ValidateAsync(string token)`.
- Created `RecaptchaOptions` in **Application** with `SecretKey` (`[Required]`) and `MinimumScore` (default 0.5) for reCAPTCHA v3 score-based evaluation.
- Implemented `RecaptchaService` in **Infrastructure** using `HttpClient` to call Google's `siteverify` API. Registered via `AddHttpClient<ICaptchaService, RecaptchaService>` with a 5-second timeout and base address `https://www.google.com/recaptcha/api/`.
- `RegisterRequest` and `LoginRequest` DTOs now include a `CaptchaToken` field sent by the client.
- `AuthService` validates the CAPTCHA token via `ValidateCaptchaAsync` before any auth logic, with the boolean return value explicitly checked (previous version discarded it).

### Alternatives Considered
- **hCaptcha**: Privacy-friendly alternative. Can be swapped by implementing a different `ICaptchaService`.
- **Honeypot fields**: Simpler but easily bypassed by sophisticated bots. Rejected as sole protection.
- **No CAPTCHA, rate-limit only**: Rate limiting helps but doesn't prevent distributed attacks. CAPTCHA + rate limiting provides defense in depth.

### Consequences
- **Positive**: Defense in depth against bots; score-based (v3) is invisible to legitimate users; logging on failed verification aids monitoring.
- **Negative**: Adds latency (HTTP call to Google on every auth request); requires client-side reCAPTCHA integration.
- **Risk**: `SecretKey` is a secret — must come from environment variable (`RECAPTCHA__SECRET_KEY`), never committed. Documented in `.env.example`.

### Validation Plan
- Runtime check: Register/login without a valid reCAPTCHA token → 401 "CAPTCHA verification failed".
- Tests to add: Unit tests for `RecaptchaService` with mocked `HttpClient` (success, failure, low score).
- Rollback: Replace `RecaptchaService` registration with a no-op `ICaptchaService` that always returns `true`.

### Implementation Notes
- Key files: `Application/Interfaces/ICaptchaService.cs`, `Application/Options/RecaptchaOptions.cs`, `Infrastructure/Services/RecaptchaService.cs`, `Infrastructure/DependencyInjection.cs`.
- Secrets: `RECAPTCHA__SECRET_KEY` in `.env` / GitHub Secrets.

- [ ] Add unit tests for `RecaptchaService`
- [ ] Create no-op `ICaptchaService` stub for local development/testing without reCAPTCHA

---

## Decision: IP-based rate limiting with per-policy configuration
Date: 2026-03-18
Status: Accepted

### Context
- Auth endpoints are a prime target for brute-force and credential stuffing attacks. Even with reCAPTCHA, rate limiting is needed as a secondary defense layer.
- General API abuse also needs to be throttled globally.

### Decision
- Created `RateLimitOptions` in **Application** with configurable limits: `AuthPermitLimit` / `AuthWindowMinutes` (auth endpoints) and `GlobalPermitLimit` / `GlobalWindowMinutes` (all endpoints). Uses `[Range]` validation.
- Created `RateLimiterExtensions.AddAuthRateLimiter()` in **Presentation** that registers:
  - A **named fixed-window limiter** (`Auth`) for auth endpoints, applied via `[EnableRateLimiting(RateLimitOptions.AuthPolicyName)]` on `AuthController`.
  - A **global IP-based partitioned limiter** for all requests using `RemoteIpAddress` as the partition key.
- Returns `429 Too Many Requests` when limits are exceeded.
- Default config: 5 auth requests/minute, 30 global requests/minute.

### Alternatives Considered
- **Sliding window**: Smoother distribution but more memory. Fixed window is simpler and sufficient for current scale.
- **Token bucket**: Better for burst tolerance. Can be adopted later if needed.
- **Reverse proxy (nginx/Cloudflare) rate limiting**: Would work but doesn't apply in development. In-app rate limiting provides consistent behavior across environments.

### Consequences
- **Positive**: Defense in depth alongside reCAPTCHA; IP-based partitioning prevents distributed abuse; configurable via `appsettings.json`.
- **Negative**: IP-based partitioning can penalize users behind shared NATs/VPNs. Acceptable risk at current scale.
- **Risk**: `RemoteIpAddress` can be `null` (falls back to `"unknown"` partition). Behind a reverse proxy, `X-Forwarded-For` must be configured — not yet done.

### Validation Plan
- Runtime check: Send > 5 rapid requests to `/api/auth/login` → 429 on the 6th.
- Tests to add: Integration test verifying 429 response after exceeding limit.
- Rollback: Remove `AddAuthRateLimiter()` call and `UseRateLimiter()` from `Program.cs`.

### Implementation Notes
- Key files: `Application/Options/RateLimitOptions.cs`, `Presentation/Extensions/RateLimiterExtensions.cs`, `Presentation/Program.cs`, `Presentation/Controllers/AuthController.cs`.
- Config: `appsettings.json` → `RateLimit` section.

- [ ] Configure `ForwardedHeaders` middleware for correct IP behind reverse proxy
- [ ] Add integration test for 429 behavior
- [ ] Consider user-ID-based partitioning for authenticated endpoints

---

## Decision: CORS policy with typed configuration
Date: 2026-03-18
Status: Accepted

### Context
- The API will be consumed by a frontend (SPA) running on a different origin (`http://localhost:5173` in development). Browsers block cross-origin requests without proper CORS headers.

### Decision
- Created `CorsOptions` in **Application** with `AuthAllowedOrigins` and `AuthAllowedMethods` arrays (`[Required]`), bound to the `Cors` config section.
- Registered a named CORS policy (`AuthCorsPolicy`) in `Program.cs` using the typed config values.
- Applied `[EnableCors(CorsOptions.AuthPolicyName)]` on `AuthController`. `AllowAnyHeader()` is permitted; origins and methods are restricted.
- Pipeline order: `ExceptionHandlingMiddleware` → `RateLimiter` → `CORS` → `Authentication` → `Authorization` → `MapControllers`.

### Alternatives Considered
- **`AllowAnyOrigin()`**: Insecure; any site could invoke the API. Rejected.
- **Global CORS via `app.UseCors()` without policy**: Would apply to all endpoints. Per-controller policy is more granular.
- **API Gateway/reverse proxy CORS**: Viable for production but doesn't help in local development. In-app CORS is needed regardless.

### Consequences
- **Positive**: Frontend can call the API; origins are restricted to configured values; methods are limited (currently `POST` only).
- **Negative**: `AllowAnyHeader()` is permissive — could be tightened to specific headers (`Content-Type`, `Authorization`).
- **Risk**: Misconfigured origins in production could block legitimate clients. Mitigated by typed config + startup validation.

### Validation Plan
- Runtime check: Frontend at `localhost:5173` can call `/api/auth/login` without CORS errors; requests from unlisted origins are blocked.
- Rollback: Remove `AddCors` / `UseCors` from `Program.cs` and `[EnableCors]` from controller.

### Implementation Notes
- Key files: `Application/Options/CorsOptions.cs`, `Presentation/Program.cs`, `Presentation/Controllers/AuthController.cs`.
- Config: `appsettings.json` → `Cors:AuthAllowedOrigins`, `Cors:AuthAllowedMethods`.

- [ ] Tighten `AllowAnyHeader()` to specific headers in production
- [ ] Add staging/production origins to config

---

## Decision: AuthService security hardening
Date: 2026-03-20
Status: Accepted

### Context
- Code review of `AuthService` revealed three security issues:
  1. `HtmlEncoder.Default.Encode()` was applied to email inputs — this is an **output** encoding technique (prevents XSS in HTML rendering) and can corrupt valid email characters (e.g. `&` → `&amp;`). EF Core uses parameterized queries, so SQL injection is not a concern.
  2. `ICaptchaService.ValidateAsync()` returns `Task<bool>` but the return value was **never checked** — a `false` result silently allowed the request through, nullifying CAPTCHA protection entirely.
  3. No security logging — failed logins, CAPTCHA failures, and rejected Google tokens produced no log entries, violating OWASP A09:2021 (Security Logging and Monitoring Failures).

### Decision
- **Removed `HtmlEncoder`** — replaced with `Trim()` only. Input format is validated by FluentValidation; persistence is safe via parameterized queries.
- **Created `ValidateCaptchaAsync` private method** that checks the boolean result and throws `UnauthorizedAccessException("CAPTCHA verification failed.")` on `false`. Centralizes the check so future callers cannot accidentally discard the result.
- **Added `ILogger<AuthService>`** with structured log messages at appropriate levels:
  - `LogWarning`: duplicate registration, failed login (unknown email / wrong password), CAPTCHA failure, invalid Google token, unverified Google email.
  - `LogInformation`: successful registration, successful Google login.
- Email addresses are **never logged** directly to avoid PII leakage — only `UserId` is included in log messages.

### Alternatives Considered
- **Keep HtmlEncoder as defense-in-depth**: Rejected — it is the wrong encoding for this context and actively corrupts data. The correct defense is parameterized queries (already in place via EF Core).
- **Throw from `ICaptchaService` directly**: Would couple the interface to exception types. Keeping it as `Task<bool>` is cleaner; the caller (`AuthService`) decides how to handle failure.

### Consequences
- **Positive**: CAPTCHA is now actually enforced; security events are observable in logs; no data corruption from misapplied encoding.
- **Negative**: None significant.
- **Risk**: Verbose logging in high-traffic scenarios. Mitigated by using `LogWarning` (not `LogInformation`) for failures — can be filtered by log level.

### Validation Plan
- Build check: `dotnet build` passes.
- Runtime check: Send request with invalid CAPTCHA → 401 "CAPTCHA verification failed" (previously passed silently).
- Tests to add: Unit test asserting `ValidateCaptchaAsync` throws when service returns `false`.
- Rollback: Revert `AuthService.cs` to previous commit.

### Implementation Notes
- Key file: `Infrastructure/Services/AuthService.cs`.
- Removed: `using System.Text.Encodings.Web`.
- Added: `using Microsoft.Extensions.Logging`, `ILogger<AuthService>` constructor parameter, `ValidateCaptchaAsync` private method.

- [ ] Add unit test for CAPTCHA failure path
- [ ] Consider structured logging sink (Seq, Application Insights) for production monitoring
- [ ] Evaluate adding account lockout after N failed login attempts (Identity supports this natively)

---

## Decision: Local host development configuration
Date: 2026-03-18
Status: Accepted

### Context
- The project was configured to run only via Docker (`docker compose up`). Running directly on the host (`dotnet run`) failed because `Jwt:SecretKey` and `Database:ConnectionString` were only available as Docker environment variables.
- Developers need to be able to run, debug, and step through code natively from their IDE.

### Decision
- Created `appsettings.Development.json` in **Presentation** with development-only secrets (`Jwt:SecretKey`, reCAPTCHA and Google placeholders) and a `Database:ConnectionString` pointing to `localhost,1433` (local SQL Server via `docker compose up sqlserver`).
- Added `appsettings.Development.json` to `.gitignore` so per-developer secrets are never committed.
- Config merge order: `appsettings.json` (committed, no secrets) → `appsettings.Development.json` (local-only, has secrets) → environment variables (Docker/CI overrides).

### Alternatives Considered
- **`dotnet user-secrets`**: Built-in secret manager. Good for individual secrets but doesn't handle connection strings as naturally as a config file. Can be adopted alongside.
- **Always run via Docker**: Ensures consistency but prevents native debugging and IDE attach. Not acceptable for day-to-day development.

### Consequences
- **Positive**: `dotnet run --project src/Presentation` works after bringing up just the SQL Server container; full IDE debugging experience.
- **Negative**: Each developer must create their own `appsettings.Development.json` from the template in `appsettings.json` + `.env.example`.
- **Risk**: Developer accidentally commits `appsettings.Development.json` with secrets. Mitigated by `.gitignore` entry.

### Validation Plan
- Runtime check: `docker compose up sqlserver -d` → `dotnet run --project src/Presentation` → Swagger UI at `http://localhost:5000/swagger`.
- Rollback: Delete `appsettings.Development.json`.

### Implementation Notes
- Key files: `Presentation/appsettings.Development.json`, `.gitignore`.
- Workflow: `docker compose up sqlserver -d` for the database, then `dotnet run` for the API.

- [ ] Add `appsettings.Development.json.example` as a committed template
- [ ] Document local setup steps in README.md