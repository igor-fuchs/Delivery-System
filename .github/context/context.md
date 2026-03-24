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

---

## Decision: CI/CD with GitHub Actions
Date: 2026-03-22
Status: Accepted

### Context
- The project had no automated build, test, or deployment pipeline. Manual deployments are error-prone and lack consistency.
- The team uses GitHub, so GitHub Actions is the natural choice for CI/CD.
- A container image needs to be published to a registry for deployment.

### Decision
- Created **CI workflow** (`.github/workflows/ci.yml`) triggered on `pull_request` to `main`/`develop` and `workflow_dispatch`. Steps: checkout → setup .NET 10.0 SDK → restore → build (Release) → run tests → upload `.trx` test results as artifact (7-day retention).
- Created **CD workflow** (`.github/workflows/cd.yml`) triggered on `workflow_dispatch` only, gated to `main` branch. Steps: checkout → login to GHCR → extract Docker metadata (SHA tag + `latest`) → build and push image using `docker/Dockerfile.prod`.
- Created **production Dockerfile** (`docker/Dockerfile.prod`) as a multi-stage build: (1) `sdk:10.0` stage restores and publishes in Release mode, (2) `aspnet:10.0` runtime stage copies publish output, runs as non-root `appuser` on port 8080.
- CI and CD are intentionally separate workflows — CI runs automatically on PRs, CD is manual-only for controlled releases.

### Alternatives Considered
- **Single workflow with conditional jobs**: Simpler but couples testing and deployment triggers. Separate workflows give independent control.
- **Azure DevOps Pipelines**: Viable but adds tool fragmentation since the repo is on GitHub.
- **Docker Hub**: GHCR integrates natively with `GITHUB_TOKEN` — no extra credentials needed.

### Consequences
- **Positive**: Every PR is validated automatically; container images are versioned by commit SHA; non-root container follows security best practices.
- **Negative**: CD requires manual dispatch — no auto-deploy on merge to `main`. Acceptable for current maturity; can add auto-trigger later.
- **Risk**: `Dockerfile.prod` must stay in sync with solution structure changes. Central Package Management files (`Directory.Build.props`, `Directory.Packages.props`) must be copied before restore.

### Validation Plan
- CI: Open a PR → workflow runs → build + tests pass → `.trx` artifact available.
- CD: Manual dispatch from `main` → image published to `ghcr.io/<owner>/delivery-system:latest`.
- Rollback: Delete the workflow files; no infrastructure changes.

### Implementation Notes
- Key files: `.github/workflows/ci.yml`, `.github/workflows/cd.yml`, `docker/Dockerfile.prod`.
- `Dockerfile.prod` copies `.csproj` files individually for layer caching, then copies `src/` for the build.
- Non-root user (`appuser`, UID 1001) in production image.

- [ ] Add auto-trigger for CD on merge to `main` when ready
- [ ] Add build caching (`actions/cache`) for faster CI runs
- [ ] Add deployment step (SSH, Kubernetes, or cloud provider) to CD workflow

---

## Decision: Docker file reorganization into `docker/` folder
Date: 2026-03-22
Status: Accepted

### Context
- Docker-related files (`Dockerfile`, `Dockerfile.prod`) were at the repository root, cluttering it alongside solution files, configs, and source directories.
- The project also has a `.devcontainer/devcontainer.json` that references the Docker Compose file.

### Decision
- Moved `Dockerfile` and `Dockerfile.prod` into a `docker/` directory at the repository root.
- `docker-compose.yml` remains at the root (Docker Compose convention and simpler `docker compose up`).
- Updated all references: `.devcontainer/devcontainer.json`, `.github/workflows/cd.yml` (`file: docker/Dockerfile.prod`), `.dockerignore`, and documentation.
- `docker/Dockerfile` (development) uses `CMD` with `dotnet restore` + `dotnet watch run` targeting `src/Presentation/Presentation.csproj`.
- Build context in `docker-compose.yml` is `.` (root), with `dockerfile: docker/Dockerfile` to pick up the relocated file.

### Alternatives Considered
- **Keep at root**: Simpler but adds visual noise. `docker/` folder is a common convention.
- **Move `docker-compose.yml` into `docker/` too**: Requires `docker compose -f docker/docker-compose.yml` everywhere. Leaving it at root is more ergonomic.

### Consequences
- **Positive**: Cleaner root directory; Docker files co-located; easy to find all container config in one folder.
- **Negative**: Every Docker reference (CI/CD, devcontainer, scripts) must use `docker/` prefix — already updated.
- **Risk**: New Dockerfiles or compose overrides must go in `docker/` to maintain consistency.

### Validation Plan
- `docker compose up --build` from root succeeds.
- CD workflow references `file: docker/Dockerfile.prod` correctly.
- DevContainer reopens successfully.

### Implementation Notes
- Key files: `docker/Dockerfile`, `docker/Dockerfile.prod`, `docker-compose.yml`, `.devcontainer/devcontainer.json`, `.github/workflows/cd.yml`.

- [ ] Update README with `docker/` folder documentation

---

## Decision: E2E integration tests with WebApplicationFactory and SQLite
Date: 2026-03-22
Status: Accepted

### Context
- The project had unit tests but no end-to-end integration tests verifying the full HTTP pipeline (routing → middleware → controller → service → database).
- Integration tests need to be fast, isolated, and runnable in CI without external dependencies (no SQL Server container).

### Decision
- Created `tests/IntegrationTests` project with `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.Sqlite`.
- Created `DeliverySystemFactory` extending `WebApplicationFactory<Program>` with:
  - **SQLite in-memory** replacing SQL Server — a single `SqliteConnection` kept open for the test lifetime, ensuring the in-memory database persists across requests.
  - **EF Core dual-provider conflict resolution** — all `ApplicationDbContext` and `DbContextOptions` registrations are removed before re-registering with SQLite, preventing EF Core's internal service provider from seeing both SQL Server and SQLite providers.
  - **FakeCaptchaService** (test-specific, controllable) replacing the real/fake CAPTCHA, allowing tests to toggle pass/fail behavior.
  - **`UseSetting`** for JWT, CORS, and RateLimit config — these values are consumed eagerly by `Program.cs` during service registration, before `ConfigureAppConfiguration` callbacks run. `UseSetting` injects them early enough.
  - **Elevated rate limits** (10,000 requests/minute) to prevent 429 responses during test runs.
  - **Role seeding** in `InitializeAsync` — creates `user` and `admin` Identity roles before tests run.
- Created `IntegrationTestBase` for shared setup (HttpClient, JSON helpers, helper methods).
- Created test classes: `RegisterEndpointTests`, `LoginEndpointTests`, `GoogleLoginEndpointTests` covering success paths, validation errors, conflict, invalid credentials, and CAPTCHA failures.
- Environment set to `"Testing"` via `UseEnvironment` to avoid Development-specific code paths (auto-migration, seeder).

### Alternatives Considered
- **SQL Server in Docker for tests**: More realistic but slow to spin up, complex CI setup, and flaky. SQLite in-memory is fast and deterministic.
- **Testcontainers**: Good for SQL Server integration but adds NuGet dependency and CI Docker-in-Docker complexity. Overkill for current needs.
- **In-memory EF Core provider**: Doesn't support relational features (constraints, transactions). SQLite is a better relational approximation.
- **ConfigureAppConfiguration for JWT/CORS/RateLimit**: Runs too late — `Program.cs` reads these eagerly. `UseSetting` is the correct mechanism for early config injection.

### Consequences
- **Positive**: Full pipeline coverage; fast (~seconds); no external dependencies; CI-friendly; controllable CAPTCHA for negative tests.
- **Negative**: SQLite doesn't match SQL Server identity behavior exactly (e.g., sequences, collation). Mitigated by testing business logic, not EF provider specifics.
- **Risk**: If `Program.cs` adds new eager config reads, they must use `UseSetting` in the factory. Documented in code comments.

### Validation Plan
- `dotnet test` runs all integration tests in CI alongside unit tests.
- All auth endpoints covered: register (success, validation, conflict, CAPTCHA), login (success, invalid, CAPTCHA), Google login (malformed JWT, CAPTCHA).
- Rollback: Delete `tests/IntegrationTests/` and remove from solution.

### Implementation Notes
- Key files: `tests/IntegrationTests/IntegrationTests.csproj`, `tests/IntegrationTests/Infrastructure/DeliverySystemFactory.cs`, `tests/IntegrationTests/Infrastructure/FakeCaptchaService.cs`, `tests/IntegrationTests/Infrastructure/IntegrationTestCollection.cs`, `tests/IntegrationTests/IntegrationTestBase.cs`, `tests/IntegrationTests/Auth/*.cs`.
- Architecture: Factory and test infrastructure in `Infrastructure/`, test classes organized by feature (`Auth/`).
- `FakeCaptchaService` in tests is separate from `Infrastructure/Services/FakeCaptchaService` — the test version has a controllable `ShouldPass` property.

- [ ] Add integration tests for authorized endpoints (once they exist)
- [ ] Add integration test for rate limiting (429) behavior
- [ ] Consider adding `TestContainers` for SQL Server-specific edge cases in a separate test suite

---

## Decision: ServiceUnavailableException (HTTP 503)
Date: 2026-03-22
Status: Accepted

### Context
- When `AuthService` calls Google's token validation endpoint and receives an `HttpRequestException` (network failure, DNS resolution, timeout), the error was propagated as an unhandled exception, resulting in a generic HTTP 500.
- Clients need to distinguish between server errors (500) and external service unavailability (503) for retry logic.

### Decision
- Created `ServiceUnavailableException` in **Application/Exceptions** (same layer as `ConflictException`, `ValidationException`) — a typed exception that signals an external dependency is unreachable.
- Registered the mapping `ServiceUnavailableException → HttpStatusCode.ServiceUnavailable (503)` in `ExceptionHandlingMiddleware` in **Presentation**.
- In `AuthService.GoogleLoginAsync`, `HttpRequestException` is caught and rethrown as `ServiceUnavailableException` with a descriptive message. `InvalidJwtException` remains mapped to `UnauthorizedAccessException` (401).

### Alternatives Considered
- **Return 500 for all external failures**: Simpler but clients cannot implement targeted retry strategies.
- **Circuit breaker (Polly)**: Would prevent cascading failures but adds complexity. Can be layered on top later.
- **Custom `ProblemDetails` without exception**: Would skip the middleware pattern. Consistency with existing exception-based flow is preferred.

### Consequences
- **Positive**: Clients can distinguish 503 (retry later) from 500 (server bug); consistent with the existing exception → status code mapping pattern.
- **Negative**: Only covers Google token validation for now. Other external calls (reCAPTCHA) should adopt the same pattern.
- **Risk**: If `ServiceUnavailableException` is thrown for transient errors, clients may retry aggressively. Consider adding `Retry-After` header in the future.

### Validation Plan
- Unit test: `GoogleLoginAsync` with `HttpRequestException` → catches `ServiceUnavailableException`.
- Integration test: Malformed Google token → 503 (when Google endpoint is unreachable).
- Rollback: Remove exception class and middleware mapping; revert `AuthService` catch.

### Implementation Notes
- Key files: `Application/Exceptions/ServiceUnavailableException.cs`, `Presentation/Middlewares/ExceptionHandlingMiddleware.cs`, `Infrastructure/Services/AuthService.cs`.

- [ ] Apply `ServiceUnavailableException` pattern to reCAPTCHA HTTP failures
- [ ] Consider adding `Retry-After` header to 503 responses
- [ ] Evaluate Polly circuit breaker for Google API calls

---

## Decision: Role-based authorization with admin seed user
Date: 2026-03-22
Status: Accepted

### Context
- All authenticated users had the same access level. The system needs at least two roles: `user` (customers) and `admin` (providers/operators).
- An initial admin user must exist to bootstrap the system without manual database manipulation.
- JWT tokens must carry role information for authorization decisions.

### Decision
- Created `AppRoles` constants in **Domain/Constants** — `User = "user"`, `Admin = "admin"`, `DefaultPolicy = "DefaultPolicy"`. Lives in Domain because roles are a core business concept.
- Created `AdminSeedOptions` in **Application/Options** — `Email` and `Password` bound to `AdminSeed` config section with `[Required]` validation. Values come from environment variables (`AdminSeed__Email`, `AdminSeed__Password`), never committed.
- Created `DatabaseSeeder` in **Infrastructure/Services** — seeds both Identity roles and the admin user on startup. Idempotent: checks existence before creating. Logs all seed operations.
- Updated `ITokenService.GenerateToken` to accept a `role` parameter (3 args: `userId`, `email`, `role`). `TokenService` includes `ClaimTypes.Role` in JWT claims.
- Updated `AuthService`:
  - `RegisterAsync`: assigns `AppRoles.User` role via `UserManager.AddToRoleAsync`.
  - `LoginAsync`: fetches user roles via `GetRolesAsync`, passes first role to `GenerateToken`.
  - `GoogleLoginAsync`: assigns `AppRoles.User` on auto-registration, fetches role on subsequent logins.
- Registered `DefaultPolicy` in `Program.cs` via `AddAuthorization` — requires either `Admin` or `User` role. Controllers can apply `[Authorize(Policy = AppRoles.DefaultPolicy)]`.
- `DatabaseSeeder.SeedAsync()` is called in the Development block of `Program.cs` after migrations.
- Updated `DependencyInjection.cs` to register `DatabaseSeeder` (scoped) and `AdminSeedOptions`.

### Alternatives Considered
- **Claims-based without Identity roles**: Flexible but reinvents role management. Identity roles integrate with `[Authorize(Roles = ...)]` natively.
- **Seed via SQL migration**: Fragile if password hashing algorithm changes. Using `UserManager` ensures consistent password hashing.
- **Seed in all environments**: Risky in production. Current implementation seeds only in Development. Production admin creation should be a separate process.

### Consequences
- **Positive**: Authorization is role-aware; admin bootstrap is automated; JWT carries role claims; `[Authorize(Policy)]` works out of the box.
- **Negative**: Only one role per user is passed to the token (first role from `GetRolesAsync`). Multi-role tokens can be added later.
- **Risk**: `DatabaseSeeder` runs only in Development. Production needs a separate admin provisioning strategy (migration script, CLI tool, or admin API).

### Validation Plan
- Runtime: Start app → roles `user` and `admin` created → admin user exists → login with admin credentials returns JWT with `role: admin`.
- Unit tests: Updated all `GenerateToken` mock setups to 3-parameter signature. `AuthServiceTests` verify `AddToRoleAsync` and `GetRolesAsync` calls.
- Integration tests: Role seeding in `DeliverySystemFactory.InitializeAsync` ensures roles exist for all test scenarios.
- Rollback: Remove `AppRoles`, `AdminSeedOptions`, `DatabaseSeeder`, revert `ITokenService` to 2-param, remove `AddAuthorization` policy.

### Implementation Notes
- Key files: `Domain/Constants/AppRoles.cs`, `Application/Options/AdminSeedOptions.cs`, `Infrastructure/Services/DatabaseSeeder.cs`, `Infrastructure/Services/TokenService.cs`, `Infrastructure/Services/AuthService.cs`, `Infrastructure/DependencyInjection.cs`, `Presentation/Program.cs`.
- Config: `AdminSeed__Email` and `AdminSeed__Password` in `.env` / `.env.example` / `docker-compose.yml`.

- [ ] Support multi-role tokens (array of roles in JWT claims)
- [ ] Create production admin provisioning strategy (CLI tool or protected endpoint)
- [ ] Add `[Authorize(Policy = AppRoles.DefaultPolicy)]` to protected endpoints as they are created
- [ ] Add admin-only endpoints with `[Authorize(Roles = AppRoles.Admin)]`

---

## Decision: FakeCaptchaService for Development environment
Date: 2026-03-22
Status: Accepted

### Context
- Running the API in Development mode required valid reCAPTCHA tokens for every register/login request, making local development and manual testing cumbersome.
- The `RecaptchaOptions` with `[Required]` `SecretKey` also forced developers to configure a real reCAPTCHA key even for local work.

### Decision
- Created `FakeCaptchaService` in **Infrastructure/Services** implementing `ICaptchaService`. Always returns `true` and logs a debug message indicating CAPTCHA was bypassed.
- Updated `DependencyInjection.AddInfrastructure` to accept `IWebHostEnvironment` (changed from `bool isDevelopment`). When `IsDevelopment()`:
  - Registers `FakeCaptchaService` as singleton (no HTTP client needed).
  - Skips `RecaptchaOptions` binding and validation (no `SecretKey` required).
- When not Development:
  - Registers `RecaptchaService` via `AddHttpClient` with the real Google endpoint.
  - Validates `RecaptchaOptions` on startup as before.
- Updated `Program.cs` to pass `builder.Environment` to `AddInfrastructure`.

### Alternatives Considered
- **Environment variable toggle**: A `CAPTCHA_ENABLED=false` flag. More flexible but error-prone — could accidentally disable CAPTCHA in production.
- **Always register real service with test key**: Still makes HTTP calls to Google, adding latency and requiring network access in development.
- **No-op middleware that strips CAPTCHA before reaching service**: Adds middleware complexity. Service-level replacement is cleaner.

### Consequences
- **Positive**: Zero-friction local development; no reCAPTCHA key needed in Development; no external HTTP calls in dev; `RecaptchaOptions` validation only runs in non-Development environments.
- **Negative**: Development and production code paths diverge — a bug in `RecaptchaService` won't surface locally.
- **Risk**: If `IsDevelopment()` check is wrong (e.g., environment variable not set), `FakeCaptchaService` could run in production. Mitigated by `IWebHostEnvironment` coming from the framework, not manual config.

### Validation Plan
- Development: Start API → register without reCAPTCHA token → succeeds (logged as bypassed).
- Production/Staging: Start API without `Recaptcha:SecretKey` → `OptionsValidationException` at startup.
- Integration tests: Use their own `FakeCaptchaService` (controllable pass/fail), independent of the infrastructure fake.
- Rollback: Revert `DependencyInjection.cs` to always register `RecaptchaService`.

### Implementation Notes
- Key files: `Infrastructure/Services/FakeCaptchaService.cs`, `Infrastructure/DependencyInjection.cs`, `Presentation/Program.cs`.
- Note: Integration tests have a separate `FakeCaptchaService` in `tests/IntegrationTests/Infrastructure/` with a toggleable `ShouldPass` property for negative testing. The infrastructure fake always passes.

- [ ] Add health check that verifies reCAPTCHA connectivity in production
- [ ] Consider a `Staging` environment that also uses `FakeCaptchaService` for QA testing