# HRReserveSystem

Курсовий проєкт за варіантом №26: **Система управління кадровим резервом (HR-система)**.

HRReserveSystem - це ASP.NET Core MVC застосунок для рекрутерів, адміністраторів та інтерв'юерів. Система веде базу кандидатів, резюме, вакансій, заявок на вакансії, співбесід, відгуків інтерв'юерів і оцінок soft skills.

## Стек технологій

- .NET 8
- ASP.NET Core MVC і Razor Views
- ASP.NET Core Identity з cookie authentication
- Entity Framework Core 8
- SQLite
- Bootstrap, jQuery validation
- xUnit integration tests

Проєкт залишається MVC-застосунком. Angular, React і Clean Architecture не використовуються.

## Запуск проєкту

```bash
dotnet restore
dotnet build
dotnet run
```

Після запуску відкрийте URL з консолі, зазвичай `http://localhost:5000` або адресу з `Properties/launchSettings.json`.

SQLite база створюється автоматично. У `Program.cs` виконується `Database.Migrate()`, тому міграції застосовуються під час старту застосунку.

## Міграції

```bash
dotnet ef migrations add НазваМіграції
dotnet ef database update
```

Для фінального hardening-етапу використовується міграція `HardeningConstraints`.

## Ролі та доступи

| Роль | Доступ |
| --- | --- |
| `Admin` | Повний доступ до кандидатів, вакансій, заявок, співбесід, відгуків, soft skills і користувачів-рекрутерів. |
| `Recruiter` | Кандидати, вакансії, заявки, співбесіди. |
| `Interviewer` | Співбесіди, відгуки інтерв'юерів, оцінки soft skills. |

CRUD-сторінки захищені атрибутами `[Authorize]`. Неавторизований користувач може відкрити сторінку входу та інформаційні сторінки.

## Демо-акаунти

| Login | Demo password | Role |
| --- | --- | --- |
| `admin` | `admin123` | Admin |
| `recruiter` | `recruiter123` | Recruiter |
| `interviewer` | `interviewer123` | Interviewer |

Демо-паролі потрібні тільки для навчального/demo-сценарію. Вони описані в `Services/DemoCredentials.cs`, використовуються для seed/demo-входу і не зберігаються в БД у відкритому вигляді.

У поточному source модель `Recruiter` використовує поле `PasswordHash`. Поля `Recruiter.Password` у предметній моделі немає. Auth flow такий: `DemoUserService` знаходить рекрутера за логіном або email, перевіряє введений пароль через `IPasswordHasher<Recruiter>` / `PasswordHasher<Recruiter>.VerifyHashedPassword`, після чого `AccountController` виконує sign-in через ASP.NET Core Identity, щоб зберегти ролі та claims у cookie.

## Реалізовані модулі

- Dashboard з основними HR-показниками.
- Кандидати: база кандидатів, пошук, фільтр за досвідом, CSV-експорт, перегляд деталей.
- Резюме: локальне завантаження PDF/DOC/DOCX до 5 MB, безпечна назва файлу через `Guid`, шлях у `ResumeFilePath`, відкриття резюме зі сторінки кандидата.
- Вакансії: опис, вимоги, зарплатний діапазон, статуси `Open`, `Paused`, `Closed`.
- Заявки: зв'язок кандидат-вакансія, етапи відбору `New`, `Screening`, `Interview`, `TestTask`, `Offer`, `Hired`, `Rejected`.
- Співбесіди: дата, тип, результат, відповідальний рекрутер, календар, `.ics` export і перехід до Google Calendar.
- Відгуки інтерв'юерів: коментар, рекомендація, оцінка від 1 до 10.
- Soft skills: оцінки комунікації, командної роботи, відповідальності, стресостійкості та лідерства від 1 до 10.
- Рекрутери: адміністрування користувачів і ролей `Admin`, `Recruiter`, `Interviewer`.
- REST API як додатковий шар до MVC: `/api/candidates`, `/api/vacancies`, `/api/applications`, `/api/interviews`, `/api/soft-skills`.

## Відповідність варіанту №26

| Вимога | Реалізація |
| --- | --- |
| База кандидатів | `CandidatesController`, модель `Candidate`, таблиця `Candidates`. |
| База резюме | `ResumeFilePath`, upload PDF/DOC/DOCX, перегляд резюме в Details. |
| Вакансії | `VacanciesController`, модель `Vacancy`, статуси вакансій. |
| Етапи відбору | `ApplicationsController`, статус заявки як етап pipeline. |
| Історія співбесід | `InterviewsController`, зв'язок зі заявками. |
| Відгуки інтерв'юерів | `InterviewFeedbacksController`, оцінка і рекомендація. |
| Оцінка soft skills | `SoftSkillAssessmentsController`, 5 оцінок і середній бал. |
| Статуси вакансій | `Open`, `Paused`, `Closed` із валідацією і DB constraint. |
| Ролі | `Admin`, `Recruiter`, `Interviewer` через ASP.NET Core Identity. |

## Посилення даних і безпеки

- Паролі рекрутерів зберігаються як `PasswordHash`; відкриті паролі в таблиці `Recruiters` не зберігаються.
- Logout виконується тільки через POST із `[ValidateAntiForgeryToken]`.
- У БД додано унікальні індекси для email/login і пари `CandidateId + VacancyId`.
- У БД додано check constraints для статусів, оцінок 1-10 і `SalaryMax >= SalaryMin`.
- Resume upload приймає PDF/DOC/DOCX до 5 MB, зберігає файл під назвою на основі `Guid` і показує резюме зі сторінки кандидата.
- Для кандидатів використовується soft delete через `IsDeleted`.
- Для вакансій використовується архівація через `IsArchived`.
- Архівні записи приховуються зі списків за замовчуванням.
- Форми мають `ValidationSummary`, `asp-validation-for` і зрозумілі українські повідомлення.

SQLite у цьому проєкті зберігає `decimal` зарплати як `TEXT`, що є типовим компромісом EF Core для SQLite. Constraint для зарплати використовує числове приведення, але для production краще зберігати зарплату як integer у копійках/центах.

## Seed data

Якщо HR-таблиці порожні, система додає:

- 3 демо-користувачі;
- 5 кандидатів;
- 3 вакансії;
- 5 заявок;
- 3 співбесіди;
- 3 відгуки;
- 3 оцінки soft skills.

Якщо база вже містить HR-дані, seed не дублює записи, а нормалізує старі статуси та гарантує наявність демо-користувачів.

## Тести та CI

Локальна перевірка:

```bash
dotnet clean
dotnet restore
dotnet build
dotnet test
```

Інтеграційні тести xUnit перевіряють login, ролі, обмеження БД, resume upload, API-відповіді та базові CRUD-сценарії. GitHub Actions workflow `.github/workflows/ci.yml` виконує:

- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test --configuration Release`

## Ручна перевірка

1. Відкрити `/Account/Login`.
2. Увійти як `admin / admin123`, перевірити Dashboard і розділ `Recruiters`.
3. Вийти через кнопку `Вийти`; logout має відправити POST-форму.
4. Увійти як `recruiter / recruiter123`, перевірити доступ до кандидатів, вакансій, заявок і співбесід.
5. Переконатися, що `Recruiter` не має доступу до `/Recruiters`.
6. Увійти як `interviewer / interviewer123`, перевірити доступ до співбесід, відгуків і soft skills.
7. Переконатися, що `Interviewer` не має доступу до `/Candidates`.
8. Створити кандидата з PDF/DOC/DOCX резюме до 5 MB.
9. Спробувати завантажити `.exe` як резюме і перевірити повідомлення про неправильний формат.
10. Створити вакансію і перевірити, що `SalaryMax < SalaryMin` не проходить.
11. Створити заявку кандидат-вакансія і перевірити, що дубльована пара не проходить.
12. Створити співбесіду, feedback і soft skills; оцінки поза діапазоном 1-10 мають блокуватися.

## Відомі обмеження

- Файли резюме зберігаються локально у `wwwroot/uploads/resumes`; хмарне сховище не використовується.
- Email-сповіщення потребують SMTP-конфігурації. Без SMTP вони записуються у `EmailOutbox`.
- Публічна реєстрація, reset password і підтвердження email не реалізовані.
