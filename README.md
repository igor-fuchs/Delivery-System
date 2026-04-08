# Delivery System

Delivery System is a backend API designed to power online ordering experiences for small food businesses such as pizzerias and snack bars.

Customers can browse products, create orders, and track their status in real time. Administrators can manage the menu, update order statuses, and oversee the entire operation through role-based permissions.

The system focuses on real-world concerns such as authentication, security, idempotent order creation, full observability, and scalability — making it a solid foundation for modern delivery platforms.

**Front-end:** [igor-fuchs/Delivery-Page](https://github.com/igor-fuchs/Delivery-Page)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| ORM | Entity Framework Core + SQL Server |
| Identity | ASP.NET Core Identity (PBKDF2) |
| Authentication | JWT Bearer + Google OAuth2 (ID Token) |
| Bot Protection | Google reCAPTCHA v3 |
| Cache / Idempotency | Redis |
| Containerization | Docker + Docker Compose |
| Testing | xUnit, NSubstitute, WebApplicationFactory |
| API Documentation | Swagger / OpenAPI |
| Observability | OpenTelemetry + Loki + Tempo + Prometheus + Mimir + Grafana |

---

## Quick Start

### With Docker (recommended)

```bash
cp .env.example .env
# edit .env with your credentials

docker compose up --build
```

| Service | URL |
|---|---|
| API | `http://localhost:8080` |
| Swagger | `http://localhost:8080/swagger` |
| Grafana | `http://localhost:3000` |

> In Development mode, EF Core migrations are applied automatically on startup.

### Locally (without observability stack)

```bash
cp .env.example .env
# edit .env with your credentials

dotnet run --project src/Presentation/Presentation.csproj
```

---

## Configuration

Copy `.env.example` to `.env` and fill in the variables:

### Application

| Variable | Description |
|---|---|
| `JWT__SECRET_KEY` | HMAC-SHA256 key for signing tokens (min 32 chars) |
| `DATABASE__CONNECTION_STRING` | SQL Server connection string |
| `DATABASE__SA_PASSWORD` | SQL Server SA password |
| `DATABASE__MSSQL_PID` | SQL Server edition (e.g. `Developer`) |
| `ADMIN_SEED__EMAIL` | Email of the admin account seeded on startup |
| `ADMIN_SEED__PASSWORD` | Admin account password |
| `RECAPTCHA__SECRET_KEY` | Google reCAPTCHA v3 secret key (production) |
| `GOOGLE__WEBCLIENTID` | OAuth2 Client ID for web clients |
| `REDIS__CONNECTION_STRING` | Redis connection string (e.g. `redis:6379`) |
| `RESEND__API_KEY` | Resend API key for sending password reset emails |
| `RESEND__FROM_EMAIL` | Sender address (must be on a verified Resend domain) |

### Observability

| Variable | Description |
|---|---|
| `OPENTELEMETRY__OTLP_ENDPOINT` | OTel Collector OTLP gRPC endpoint (default: `http://otel-collector:4317`) |
| `GRAFANA__ADMIN_USER` | Grafana admin username (min 3 chars) |
| `GRAFANA__ADMIN_PASSWORD` | Grafana admin password (min 8 chars) |

---

## Architecture

Clean Architecture in 4 layers — dependencies always point inward:

```
Presentation  →  Application  →  Domain
Infrastructure  →  Application  →  Domain
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, constants, domain exceptions |
| **Application** | Service interfaces, DTOs, validators, options, application exceptions |
| **Infrastructure** | EF Core, ASP.NET Identity, JWT, reCAPTCHA, Google OAuth2, telemetry |
| **Presentation** | Controllers, middlewares, filters, Program.cs |

---

## Observability

The full LGTM stack ships with the project and starts automatically with `docker compose up`.

```
API  ──OTLP gRPC──►  OTel Collector
                         ├── Logs    ──►  Loki   :3100
                         ├── Traces  ──►  Tempo  :3200
                         └── Metrics ──►  Prometheus :9090  ──remote_write──►  Mimir :19009
                                                                                    ▲
Grafana :3000  ◄── queries ─────────────────────────────────────────────────────────┘
```

- **W3C Trace Context** is propagated automatically on all incoming and outgoing HTTP requests.
- **PII protection**: emails, passwords, tokens, and path parameters are never written to logs. Route templates (`/api/orders/{id}`) are used instead of actual paths.
- **Grafana datasources** (Loki, Tempo, Prometheus, Mimir) are auto-provisioned on first boot — no manual setup required.
- Logs include a `TraceId` field that Grafana uses to link directly from a log line to its trace in Tempo.

See [docs/guides/grafana-setup.md](docs/guides/grafana-setup.md) for login instructions, LogQL/PromQL examples, and recommended dashboards.

---

## Endpoints

### Auth — `/api/auth`

| Method | Route | Description | Auth |
|---|---|---|---|
| POST | `/register` | Create a new user account | No |
| POST | `/login` | Login with email and password | No |
| POST | `/google` | Login with Google ID Token | No |
| POST | `/forgot-password` | Request a password reset email | No |
| POST | `/reset-password` | Reset password using the received token | No |

### Products — `/api/products`

| Method | Route | Description | Auth |
|---|---|---|---|
| GET | `/` | List all products | User / Admin |
| GET | `/{id}` | Get product by ID | User / Admin |
| POST | `/` | Create a new product | Admin |
| PUT | `/{id}` | Update a product | Admin |
| DELETE | `/{id}` | Delete a product | Admin |

### Orders — `/api/orders`

| Method | Route | Description | Auth |
|---|---|---|---|
| GET | `/` | List orders (admin sees all, user sees own) | User / Admin |
| GET | `/{id}` | Get order by ID | Owner / Admin |
| POST | `/` | Create a new order (requires `Idempotency-Key`) | User / Admin |
| PUT | `/{id}` | Update order status | Admin |
| DELETE | `/{id}` | Delete an order | Admin |

---

## Tests

```bash
# All tests
dotnet test --configuration Release

# Unit tests only
dotnet test tests/UnitTests/UnitTests.csproj

# Integration tests only
dotnet test tests/IntegrationTests/IntegrationTests.csproj

# Filter by name
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

Integration tests use SQLite in-memory (replacing SQL Server) and `FakeCaptchaService`. The full observability stack is not required — the OTel SDK exports silently when the collector is unreachable.

---

## Documentation

The `docs/` folder is organized into two sections:

### `docs/guides/` — Setup guides

Step-by-step instructions for configuring external services and running project tooling.

| Guide | Description |
|---|---|
| [Grafana observability](docs/guides/grafana-setup.md) | Full setup guide: login, datasources, LogQL/PromQL, trace-log correlation, dashboards |
| [Database migrations](docs/guides/migration-database.md) | How to create and apply EF Core migrations |
| [Google reCAPTCHA v3](docs/guides/recaptcha.md) | How to obtain and configure reCAPTCHA keys |
| [Google OAuth 2.0](docs/guides/google-oauth2-setup.md) | How to create OAuth2 credentials for web and Android clients |
| [Resend email](docs/guides/resend-setup.md) | How to generate a Resend API key and configure the email sender |

### `docs/project/` — Project reference

Technical reference documents describing how the project is built and behaves.

| Document | Description |
|---|---|
| [Architecture](docs/project/architecture.md) | Layer responsibilities, request flow, auth flows, and testing strategy |
| [Database schema](docs/project/database.md) | All tables, columns, relationships, and design decisions |
| [Error codes](docs/project/error-codes.md) | All error codes, HTTP mappings, and validation field codes |
