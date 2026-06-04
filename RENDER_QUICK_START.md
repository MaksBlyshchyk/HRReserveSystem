# Render.com: Розгортання за 5 хвилин

## ⚡ Найбільш швидкий спосіб

### 1️⃣ Предумова: GitHub
- Весь код має бути в GitHub репозиторію
- Запхати останні зміни:
  ```bash
  git add .
  git commit -m "Ready for Render deployment"
  git push origin main
  ```

### 2️⃣ Чекаутер на Render
1. Перейти: https://render.com
2. Натиснути "Sign up"
3. Вибрати "GitHub"
4. Авторизуватися

### 3️⃣ Розпочати розгортання
1. На Render Dashboard: **"New +" → "Web Service"**
2. Вибрати репозиторій: `HRReserveSystem`
3. Вибрати гілку: `main`
4. Натиснути **"Deploy"**

### 4️⃣ Очікування
Render почне:
- `Building Docker image...` (2 хвилини)
- `Pushing image...` (1 хвилина)
- `Deploying...` (1 хвилина)

Всього ~5 хвилин. Статус змінюється на **"Live"**.

### 5️⃣ Готово!
Відкрити браузер на URL:
```
https://hrreservesystem-xxx.onrender.com
```

---

## 🤔 Що робити з Postgres?

### Варіант A: Render Postgres (рекомендовано)
На Render Dashboard:
1. Натиснути **"New +" → "PostgreSQL"**
2. Вибрати "Free"
3. Render автоматично створить Postgres
4. Скопіювати `DATABASE_URL` з деталей БД
5. Додати в Web Service environment variables:
   ```
   DATABASE_PROVIDER=PostgreSQL
   DATABASE_URL=postgres://user:password@host:5432/dbname
   ```
6. Перезапустити Web Service

### Варіант B: Локальна Postgres (якщо Render нема)
Додаток налаштований на автоматичне підключення до `postgres://localhost` (за замовчуванням). Якщо Postgres не доступна, додаток спробує зваліти, але Render покладаються на health checks.

**Варіант A простіше для захисту.**

---

## 🔍 Перевірити, що всі добре

1. Браузер: https://hrreservesystem-xxx.onrender.com
2. Дашборд має завантажитися (можливо, це займе 30 сек на free tier)
3. Логування (на Render Dashboard → Web Service → Logs)

---

## 📊 Якщо щось не вийшло

| Симптом | Що робити |
|---------|-----------|
| `Health check failed` | Перевірити логи; можливо, БД не доступна |
| `Build failed` | Перевірити Dockerfile; локально запустити `docker build` |
| `503 Service Unavailable` | Render спить на free tier; чекати 30 сек і оновити браузер |
| `Connection refused` | DATABASE_URL не правильна; перевірити environment vars |

---

## 🎓 Для захисту

На виділеному комп'ютері:
```
1. Натиснути F5 на браузері з URL: https://hrreservesystem-xxx.onrender.com
2. Дашборд має завантажитися
3. Клікати по розділам (вакансії, кандидати, заявки)
4. Всі дані на хмарі — HDMI кабель завжди підключений ✓
```

---

## 💡 Порада

- Зберігай URL куди-небудь (закладки браузера)
- Звинувачи Render, якщо що-небудь сломається ( JK, це безпатна послуга 😄)
- На free tier додаток "засинає" після 15 хвилин неактивності — першій запит його розбудить

