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

O `.env` já vem com valores funcionais para desenvolvimento local. A connection string aponta para o container `sqlserver` definido no `docker/docker-compose.yml`.

> **O arquivo `.env` é gitignored e nunca deve ser commitado.**

---

## Subindo os containers

```bash
docker compose -f docker/docker-compose.yml up --build
```

Isso irá:
1. Subir o **SQL Server 2022** no container `sqlserver`
2. Aguardar o SQL Server ficar saudável (healthcheck)
3. Subir a **API** com `dotnet watch run` (hot reload ativo)

## Endpoints disponíveis

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/auth/register` | Registra um novo usuário |
| `POST` | `/api/auth/login` | Autentica e retorna um JWT |

A documentação interativa (Swagger UI) está disponível em `http://localhost:8080` em ambiente `Development`.
