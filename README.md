# Delivery System

Delivery System is a backend API designed to power online ordering experiences for small food businesses such as pizzerias and snack bars.

The goal is to provide a simple and reliable way for customers to place orders through web or mobile applications, while giving business owners full control over order management.

Customers can browse products, create orders, and track their status in real time. On the other side, administrators can manage the menu, update order statuses, and oversee the entire operation through role-based permissions.

The system focuses on real-world concerns such as authentication, security, idempotent order creation, and scalability — making it a solid foundation for modern delivery platforms.

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

---

## Quick Start

### With Docker (recommended)

```bash
cp .env.example .env
# edit .env with your credentials

docker compose up --build
```

The API will be available at `http://localhost:8080` and Swagger at `http://localhost:8080/swagger`.

### Locally

```bash
cp .env.example .env
# edit .env with your credentials

dotnet run --project src/Presentation/Presentation.csproj
```

> In Development mode, EF Core migrations are applied automatically on startup.

---

## Configuration

Copy `.env.example` to `.env` and fill in the variables:

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
| `RESEND__API_KEY` | Resend API key for sending password reset emails (production) |
| `RESEND__FROM_EMAIL` | Sender address shown to recipients (must be on a verified Resend domain) |

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
| **Infrastructure** | EF Core, ASP.NET Identity, JWT, reCAPTCHA, Google OAuth2 |
| **Presentation** | Controllers, middlewares, filters, Program.cs |

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

Integration tests use SQLite in-memory (replacing SQL Server) and `FakeCaptchaService`.

---

## Documentation

The `docs/` folder is organized into two sections:

### `docs/guides/` — Setup guides
Step-by-step instructions for configuring external services and running project tooling. Intended for developers setting up the project for the first time.

| Guide | Description |
|---|---|
| [Database migrations](docs/guides/migration-database.md) | How to create and apply EF Core migrations |
| [Google reCAPTCHA v3](docs/guides/recaptcha.md) | How to obtain and configure reCAPTCHA keys |
| [Google OAuth 2.0](docs/guides/google-oauth2-setup.md) | How to create OAuth2 credentials for web and Android clients |
| [Resend email](docs/guides/resend-setup.md) | How to generate a Resend API key and configure the email sender |

### `docs/project/` — Project reference
Technical reference documents describing how the project is built and behaves. Useful for understanding the system before making changes.

| Document | Description |
|---|---|
| [Architecture](docs/project/architecture.md) | Layer responsibilities, request flow, auth flows, and testing strategy |
| [Database schema](docs/project/database.md) | All tables, columns, relationships, and design decisions |
| [Error codes](docs/project/error-codes.md) | All error codes, HTTP mappings, and validation field codes |
