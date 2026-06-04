# HRReserveSystem

Курсовий проєкт за варіантом №26: **система управління кадровим резервом (HR-система)**.

HRReserveSystem - це ASP.NET Core MVC застосунок для демонстрації роботи рекрутингової системи. Він веде кандидатів, резюме, вакансії, заявки на вакансії, співбесіди, відгуки інтерв'юерів, оцінки soft skills і ролі користувачів.

---

## 🚀 Швидкий старт для захисту

### Локально (на твоєму ПК)
Див. [LAUNCH.md](LAUNCH.md) — 3 команди для старту з Docker Compose.

### На Render.com (хмара — рекомендовано для захисту)
Див. [DEPLOYMENT.md](DEPLOYMENT.md) — розгортання на безпатному tier з автоматичним розгортанням з GitHub.

**Чому на хмарі краще:** Жодних залежностей від локального комп'ютера, стабільна демонстрація, кодове слово `zhukovskyy` — просто відкрити браузер на URL на виділеному комп'ютері.

---

## Стек

- .NET 8
- ASP.NET Core MVC, Razor Views
- ASP.NET Core Identity, cookie authentication
- Entity Framework Core 8
- SQLite для локального демо
- PostgreSQL як production-провайдер через конфігурацію
- Bootstrap, jQuery validation
- xUnit integration tests
- Docker / Docker Compose

Проєкт залишається MVC-застосунком. Angular, React і повна перебудова архітектури не використовуються.

## Функціонал

- Dashboard з HR-показниками.
- Кандидати: CRUD, пошук, фільтр за досвідом, CSV export, soft delete, відновлення.
- Резюме: upload PDF/DOC/DOCX до 5 MB, збереження під GUID-назвою, download тільки з `wwwroot/uploads/resumes`.
- Вакансії: CRUD, статуси `Open`, `Paused`, `Closed`, архівація і відновлення.
- Заявки: зв'язок кандидат + вакансія, pipeline-статуси `New`, `Screening`, `Interview`, `TestTask`, `Offer`, `Hired`, `Rejected`.
- Співбесіди: дата, тип, результат, відповідальний рекрутер, `.ics` export, Google Calendar link.
- Email/Outbox: повідомлення після створення або редагування співбесіди.
- Відгуки інтерв'юерів: коментар, рекомендація, оцінка 1-10.
- Soft skills: 5 оцінок 1-10 і середній бал.
- REST API: `/api/candidates`, `/api/vacancies`, `/api/applications`, `/api/interviews`, `/api/soft-skills`.
- Health checks: `/health/live`, `/health/ready`.

## Ролі

| Роль | Доступ |
| --- | --- |
| `Admin` | Повний доступ до кандидатів, вакансій, заявок, співбесід, відгуків, soft skills і рекрутерів. |
| `Recruiter` | Кандидати, вакансії, заявки, співбесіди. |
| `Interviewer` | Співбесіди, відгуки, soft skills. |

Неавторизований користувач може відкрити сторінку входу та інформаційні сторінки. API без авторизації повертає `401`, а користувач без потрібної ролі - `403`.

## Демо-акаунти

| Login | Password | Role |
| --- | --- | --- |
| `admin` | `admin123` | Admin |
| `recruiter` | `recruiter123` | Recruiter |
| `interviewer` | `interviewer123` | Interviewer |

Демо-паролі потрібні тільки для навчального сценарію. У моделі `Recruiter` зберігається `PasswordHash`, відкриті паролі в БД не зберігаються.

## Локальний запуск

```bash
dotnet restore
dotnet build
dotnet run
```

За замовчуванням використовується SQLite:

```json
"Database": {
  "Provider": "SQLite"
}
```

SQLite-файл `hrreserve.db` створюється локально і не комітиться. У `Program.cs` виконується `Database.Migrate()`, тому міграції застосовуються під час старту застосунку.

## Запуск тестів

```bash
dotnet clean
dotnet restore
dotnet build
dotnet test
```

Integration tests перевіряють login, ролі, candidates, duplicate email, duplicate application `CandidateId + VacancyId`, score range, resume upload, API DTO без password fields, health endpoints та EmailOutbox fallback.

## Upload Резюме

Ручна перевірка:

1. Увійти як `recruiter / recruiter123`.
2. Відкрити кандидата або створити нового кандидата.
3. Завантажити `.pdf`, `.doc` або `.docx` до 5 MB.
4. Переконатися, що файл відкривається зі сторінки кандидата.
5. Спробувати `.exe` або файл понад 5 MB - система має відхилити upload.

Безпека upload:

- дозволені тільки PDF/DOC/DOCX;
- максимальний розмір - 5 MB;
- ім'я файлу генерується через `Guid`;
- posted `ResumeFilePath` ігнорується;
- download працює тільки для шляхів `/uploads/resumes/...`;
- `wwwroot/uploads/` не комітиться.

## Email І EmailOutbox

За замовчуванням `Email:Enabled=false`, тому реальні листи не надсилаються. Повідомлення зберігається як `.txt` у `EmailOutbox`.

SMTP можна налаштувати через `appsettings`, user secrets або environment variables:

```bash
Email__Enabled=true
Email__Host=smtp.example.com
Email__Port=587
Email__UserName=mailer@example.com
Email__Password=change-me
Email__FromEmail=no-reply@example.com
Email__FromName="HR Reserve System"
```

Для QA можна вказати:

```bash
Email__RedirectAllTo=qa@example.com
```

Тоді всі повідомлення підуть на одну тестову адресу. `EmailOutbox/` не комітиться.

## API

API використовує ту саму cookie-авторизацію, що й MVC.

Після входу через браузер можна перевірити:

- `/api/candidates`
- `/api/vacancies`
- `/api/applications`
- `/api/interviews`
- `/api/soft-skills`

DTO не містять `Password`, `PasswordHash` або інших password fields.

## Docker

```bash
docker compose build
docker compose up
```

Docker Compose запускає:

- web app;
- PostgreSQL.

У compose для локального HTTP-демо вимкнено HTTPS redirect і cookie secure policy переведено в `SameAsRequest`. Для реального production розміщення використовуйте TLS termination/reverse proxy і production defaults. Деталі: `DEPLOYMENT.md`.

## Health Checks

- `/health/live` - застосунок запущений;
- `/health/ready` - застосунок може підключитися до БД.

## CI

GitHub Actions workflow `.github/workflows/ci.yml` виконує:

- `dotnet restore`
- `dotnet build --configuration Release --no-restore`
- `dotnet test --configuration Release --no-build`
- `docker build`

## Known Limitations

- Це production-ready MVP для демонстрації, не повна enterprise HR-платформа.
- Публічна реєстрація, reset password і підтвердження email не реалізовані.
- Файли резюме зберігаються локально, хмарне сховище не підключене.
- PostgreSQL підтримується як production-провайдер через окремий `PostgresApplicationDbContext` і PostgreSQL migrations у `Migrations/Postgres`; перед реальним production запуском треба перевірити міграції на staging БД і зробити backup.
- SQLite зберігає salary decimal як `TEXT`, це локальний компроміс для демо.

## Database provider notes and how to avoid schema drift

- Default for local development: `SQLite` (see `appsettings.json`). SQLite stores `decimal` values as `TEXT` in this demo to avoid precision differences locally.
- Production / Docker: `PostgreSQL` (compose sets `DATABASE_PROVIDER=PostgreSQL`). PostgreSQL should use `numeric` for `decimal` values.

Problem that may occur:
- If you create or apply migrations targeted at SQLite, columns like `Vacancies.SalaryMin` / `SalaryMax` may end up with `type: "TEXT"` in migrations/snapshots. Applying those migrations to PostgreSQL will create text columns and later EF Core will throw `InvalidCastException` when materializing `decimal` properties.

How to avoid this (recommended workflow):
1. Decide the provider for the environment you are targeting (local vs staging vs production).
2. When working with PostgreSQL, use the `PostgresApplicationDbContext` and keep Postgres migrations in `Migrations/Postgres/` only.
  - Create Postgres migrations with the explicit context and output dir, for example:

```powershell
dotnet ef migrations add AddExample --context PostgresApplicationDbContext --output-dir Migrations/Postgres
```

3. Inspect generated migrations for column types. For Postgres numeric/decimal columns you should see `type: "numeric(18,2)"` (or `numeric`). If you see `type: "TEXT"`, update the migration to use the correct numeric type before applying.

4. Always test migrations against a staging database and create a backup before applying to production:

```bash
pg_dump -h <host> -U <user> -d <db> -F c -f ./backups/hrreserve-before-migration.dump
```

5. If you already have a Postgres DB with `SalaryMin/SalaryMax` as text, prefer running a safe conversion script (create temporary numeric columns, convert values, validate, then rename) rather than `ALTER COLUMN ... USING` directly. See `DEPLOYMENT.md` and the `docker-compose.yml` for the production runbook.

6. After applying manual SQL changes to the DB, create a small EF migration (or update the Postgres snapshot) so the EF Core model snapshot matches the actual DB schema. This keeps future scaffolds and migrations correct.

Quick diagnostics (run on the Postgres host):

```sql
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name = 'vacancies' AND column_name IN ('salarymin','salarymax');

SELECT "Id","Title","SalaryMin","SalaryMax"
FROM "Vacancies"
LIMIT 20;
```

If you'd like, I can prepare:
- A safe SQL script to convert `SalaryMin`/`SalaryMax` to `numeric(18,2)` (requires backup and review); or
- An EF Core migration and the required snapshot updates so changes are tracked by EF.

Keeping a single authoritative migrations folder per provider (`Migrations` for SQLite demo, `Migrations/Postgres` for Postgres) and following the steps above prevents these schema drift issues.
