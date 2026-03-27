# Database Migrations

## Applying Migrations

### Option 1 — Automatic (recommended)

In `Development` mode, the API automatically applies pending migrations on startup. Simply run the containers and the database will be ready.

```bash
docker compose up --build
```

### Option 2 — Manual via CLI

With the containers running (or a local SQL Server available), run from the project root with the `.env` variables exported:

```bash
export $(grep -v '^#' .env | xargs)

dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj
```

---

## Creating a New Migration

After modifying any entity or `ApplicationDbContext`:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj \
  --output-dir Data/Migrations
```

---

## Reverting the Last Migration

```bash
dotnet ef migrations remove \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj
```

> `migrations remove` only removes the last unapplied migration. If the migration has already been applied to the database, run `dotnet ef database update <PreviousMigrationName>` first to roll back.
