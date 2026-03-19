# migration-database

## Gerando o banco de dados

### Opção 1 — Automático (recomendado)

O banco é criado e as migrations são aplicadas automaticamente no startup da API em ambiente `Development`. Basta subir os containers que o banco já estará pronto.

### Opção 2 — Manual via CLI

Com os containers rodando, execute a partir do devcontainer ou da máquina host (com .NET SDK instalado):

```bash
# Certifique-se que as variáveis do .env estão disponíveis
export $(grep -v '^#' .env | xargs)

dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj
```

### Criando uma nova migration

Após alterar entidades ou o `ApplicationDbContext`:

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj \
  --output-dir Data/Migrations
```

### Revertendo a última migration

```bash
dotnet ef migrations remove \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Presentation/Presentation.csproj
```

---

## Tabelas criadas pelo Identity

| Tabela | Descrição |
|---|---|
| `AspNetUsers` | Usuários do sistema (inclui `Name` e `CreatedAt`) |
| `AspNetRoles` | Roles de autorização |
| `AspNetUserRoles` | Vínculo N:N entre usuário e role |
| `AspNetUserClaims` | Claims associadas a usuários |
| `AspNetRoleClaims` | Claims associadas a roles |
| `AspNetUserLogins` | Provedores externos (OAuth) |
| `AspNetUserTokens` | Tokens de refresh e 2FA |

---