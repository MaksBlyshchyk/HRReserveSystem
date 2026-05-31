# DEPLOYMENT

Коротка інструкція для запуску HRReserveSystem як production-ready MVP.

## Локальний запуск

```bash
dotnet restore
dotnet build
dotnet run
```

Локально використовується SQLite з `appsettings.json`:

```bash
DATABASE_PROVIDER=SQLite
ConnectionStrings__DefaultConnection="Data Source=hrreserve.db"
```

## Production Запуск

Мінімальні environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Production
DATABASE_PROVIDER=PostgreSQL
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=hrreserve;Username=hrreserve;Password=change-me;Include Error Detail=false"
Email__Enabled=false
Email__Host=
Email__Port=587
Email__UserName=
Email__Password=
Email__FromEmail=no-reply@example.com
Email__FromName="HR Reserve System"
Email__RedirectAllTo=
```

`DATABASE_URL` також підтримується:

```bash
DATABASE_PROVIDER=PostgreSQL
DATABASE_URL=postgres://hrreserve:change-me@localhost:5432/hrreserve
```

У Production за замовчуванням увімкнені HSTS, HTTPS redirection і secure cookies. Якщо застосунок стоїть за reverse proxy, TLS має завершуватися на proxy, а forwarded headers/HTTPS policy треба налаштувати на рівні інфраструктури.

## Docker Compose

1. Скопіювати `.env.example` у `.env`.
2. Заповнити `POSTGRES_PASSWORD`.
3. Запустити:

```bash
docker compose build
docker compose up
```

Compose запускає web app і PostgreSQL. Застосунок сам виконує `Database.Migrate()` під час старту. Для реального production перед оновленням версії зробіть backup БД.

## База Даних

SQLite:

- використовується для локального демо;
- файл `hrreserve.db` не комітиться;
- міграції лежать у `Migrations`.

PostgreSQL:

- вмикається через `DATABASE_PROVIDER=PostgreSQL`;
- connection string передається через `ConnectionStrings__DefaultConnection` або `DATABASE_URL`;
- PostgreSQL migrations знаходяться у `Migrations/Postgres`;
- перед production запуском перевірте міграції на staging БД.

## Email

Fallback режим:

```bash
Email__Enabled=false
```

У цьому режимі повідомлення створюються в `EmailOutbox`.

SMTP режим:

```bash
Email__Enabled=true
Email__Host=smtp.example.com
Email__Port=587
Email__UserName=mailer@example.com
Email__Password=change-me
Email__FromEmail=no-reply@example.com
Email__FromName="HR Reserve System"
```

Для тестування:

```bash
Email__RedirectAllTo=qa@example.com
```

## Backup

SQLite:

```bash
copy hrreserve.db backups\hrreserve-YYYYMMDD.db
```

PostgreSQL:

```bash
pg_dump -h localhost -U hrreserve -d hrreserve -F c -f backups/hrreserve-YYYYMMDD.dump
```

Docker Compose PostgreSQL:

```bash
docker compose exec postgres pg_dump -U hrreserve -d hrreserve -F c -f /tmp/hrreserve.dump
docker compose cp postgres:/tmp/hrreserve.dump ./backups/hrreserve-YYYYMMDD.dump
```

## Health Checks

```bash
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready
```

- `/health/live` перевіряє, що процес застосунку живий.
- `/health/ready` перевіряє доступність налаштованої БД.

## Секрети

Не комітьте:

- `.env`;
- SMTP passwords;
- Gmail app passwords;
- токени;
- приватні ключі;
- реальні резюме;
- `EmailOutbox`;
- `DataProtectionKeys`.
