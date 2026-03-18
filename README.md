# Delivery System

## Pré-requisitos

- [Docker](https://www.docker.com/) e Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (para rodar fora do container)

---

## Configuração do ambiente

Copie o arquivo de exemplo e preencha as variáveis:

```bash
cp .env.example .env
```

O `.env` já vem com valores funcionais para desenvolvimento local. A connection string aponta para o container `sqlserver` definido no `docker-compose.yml`.

> **O arquivo `.env` é gitignored e nunca deve ser commitado.**

---

## Subindo os containers

```bash
docker compose up --build
```

Isso irá:
1. Subir o **SQL Server 2022** no container `sqlserver`
2. Aguardar o SQL Server ficar saudável (healthcheck)
3. Subir a **API** com `dotnet watch run` (hot reload ativo)

---

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

## Endpoints disponíveis

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/auth/register` | Registra um novo usuário |
| `POST` | `/api/auth/login` | Autentica e retorna um JWT |

A documentação interativa (Swagger UI) está disponível em `http://localhost:8080` em ambiente `Development`.
