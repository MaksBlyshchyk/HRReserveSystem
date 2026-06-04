# DEPLOYMENT — Інструкції для розгортання

## Швидко: Локальний запуск для демонстрації

Див. [LAUNCH.md](LAUNCH.md) — найпростіші 3 кроки.

---

## Production: Розгортання на Render.com

### Чому Render?
- **Безпатний** tier з PostgreSQL
- **Автоматичне розгортання** з GitHub
- **Жодних налаштувань** — просто push код
- **Stабільна демонстрація** на захисті (інтернет + хмара)

### Крок за кроком

1. **GitHub:** Переконатись, що весь код закомітлено
   ```bash
   git add .
   git commit -m "Ready for deployment"
   git push origin main
   ```

2. **Render:** Перейти на https://render.com, реєстрація через GitHub

3. **Розгортання:** 
   - Натиснути "New Web Service"
   - Вибрати GitHub репозиторій
   - Render прочитає `render.yaml` і розгорне все автоматично

4. **Чекати:** Після успіху отримаєш URL: `https://hrreservesystem.onrender.com`

5. **Демонстрація:** На виділеному комп'ютері просто відкрити браузер на цю URL

---

## Локальний запуск (без Docker)

Для development:

```bash
# Встановити .NET 8 SDK (https://dotnet.microsoft.com/download)

# Встановити Postgres локально або розпочати контейнер:
docker run -d \
  --name postgres-dev \
  -e POSTGRES_DB=hrreserve \
  -e POSTGRES_USER=hrreserve \
  -e POSTGRES_PASSWORD=change-me \
  -p 5432:5432 \
  postgres:16-alpine

# Запустити додаток:
dotnet run
```

---

## Docker Compose (локально + Postgres у Docker)

```bash
docker compose up -d
dotnet run --configuration Release
```

Див. [LAUNCH.md](LAUNCH.md) для деталей.

---

## Environment Variables

**Development (локально):**
```bash
DATABASE_PROVIDER=SQLite
```

**Production (Render/Docker):**
```bash
ASPNETCORE_ENVIRONMENT=Production
DATABASE_PROVIDER=PostgreSQL
DATABASE_URL=postgres://user:password@host:5432/dbname
# Або:
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
```

Інші необов'язкові:
```bash
Email__Enabled=false
Email__Host=smtp.example.com
Email__Port=587
Email__UserName=your-email@example.com
Email__Password=your-password
Email__FromEmail=no-reply@example.com
Email__FromName=HR Reserve System
```

---

## Міграції БД

### Автоматичні (при старті):
```csharp
// У Program.cs:
dbContext.Database.Migrate();
```

Це запускається автоматично при старті додатка.

### Вручну (якщо потрібно):
```bash
dotnet ef database update --context PostgresApplicationDbContext
```

### Генерувати нову міграцію:
```bash
dotnet ef migrations add MigrationName --context PostgresApplicationDbContext --output-dir Migrations/Postgres
```

---

## Здоровлення (Health Checks)

Додаток має health check endpoints:
- `GET /health/live` — чи живий додаток
- `GET /health/ready` — чи готовий (БД підключена, міграції застосовані)

Render використовує `/health/ready` для перевірки, чи розгортання успішне.

---

## Логування

Логи пишуться в консоль. На Render можна переглядати в Dashboard → Logs.

---

## Проблеми?

| Проблема | Рішення |
|----------|---------|
| `Build failed` | Запустити локально `dotnet build` та перевірити помилки |
| `Database error` | Переконатись, що PostgreSQL запущена і доступна |
| `Migrations failed` | Перевірити, чи міграції застосовуються без помилок локально |
| Старих даних на Render | Render має свою БД; локальні дані на твоєму комп'ютері не синхронізуються |

---

## Деталі конфігурації

- **Файли конфігурації:** `appsettings.json`, `appsettings.Production.json`
- **Docker:** `Dockerfile`, `docker-compose.yml`
- **Render:** `render.yaml`
- **Старт:** `Program.cs` (конфігурація всього у `Startup`)

Див. детально код у `Services/DatabaseConfiguration.cs`.
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
