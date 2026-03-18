# Engineering Decision Log

---

## Decision: User authentication with JWT (Login & Register)
Date: 2026-03-14
Status: Accepted

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