# Deployment

## Production Configuration

Local development uses SQLite by default:

```bash
dotnet run
```

Production and Docker deployments should use PostgreSQL:

```bash
Database__Provider=PostgreSQL
ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=hrreserve;Username=hrreserve;Password=change-me;Include Error Detail=false"
ASPNETCORE_ENVIRONMENT=Production
```

`DATABASE_URL` is also supported for PostgreSQL-style URLs:

```bash
DATABASE_PROVIDER=PostgreSQL
DATABASE_URL=postgres://hrreserve:change-me@postgres:5432/hrreserve
```

Do not commit real passwords or SMTP credentials. Start from `.env.example` and create a local `.env` file for Docker Compose.

## Docker Compose

1. Copy `.env.example` to `.env`.
2. Change `POSTGRES_PASSWORD`.
3. Start the stack:

```bash
docker compose up --build
```

The app listens on `http://localhost:8080` by default. Change `APP_PORT` in `.env` if the port is busy.

The compose stack creates named volumes for PostgreSQL data, ASP.NET Core Data Protection keys, email outbox files, and uploaded resumes.

## Health Checks

The application exposes:

- `/health/live` - process liveness, does not require a database connection;
- `/health/ready` - readiness, checks the configured database connection.

Use `/health/live` for container liveness probes and `/health/ready` for readiness/load balancer checks.

## Migrations And Seed Data

On startup the application runs `Database.Migrate()` and then `SeedData.InitializeAsync(...)`.

This keeps local and Docker startup simple, but production operators should still back up the database before deploying a new version with migrations.

## Logging

The app uses built-in console/debug logging and ASP.NET Core HTTP logging for method, path, status code, and duration. Sensitive request bodies and headers are not logged by this configuration.

Useful production overrides:

```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Warning
```

## Email

Email is disabled by default. Configure SMTP only when real sending is needed:

```bash
Email__Enabled=true
Email__Host=smtp.example.com
Email__Port=587
Email__UseSsl=true
Email__UserName=mailer@example.com
Email__Password=change-me
Email__FromEmail=no-reply@example.com
Email__FromName="HR Reserve System"
```

For QA environments, `Email__RedirectAllTo=qa@example.com` sends all messages to one test mailbox.
