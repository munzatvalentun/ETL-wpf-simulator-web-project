# ETL Simulator — Web + WPF

Навчальний дипломний проєкт, що демонструє повний **ETL-пайплайн** (Extract → Transform → Load) із тришаровою архітектурою сховища даних (Bronze / Silver / Gold) та веб-інтерфейсом моніторингу.

---

## Зміст

- [Огляд архітектури](#огляд-архітектури)
- [Структура рішення](#структура-рішення)
- [Схема бази даних](#схема-бази-даних)
- [ETL-пайплайн — детально](#etl-пайплайн--детально)
- [WPF-симулятор](#wpf-симулятор)
- [Веб-додаток](#веб-додаток)
- [Ролі користувачів](#ролі-користувачів)
- [Зв'язок між проєктами (Named Pipe)](#звязок-між-проєктами-named-pipe)
- [Юніт-тести](#юніт-тести)
- [Запуск з нуля](#запуск-з-нуля)

---

## Огляд архітектури

```
┌────────────────────────────────────────────────────────────────┐
│                        SQL Server (EtlDb)                      │
│                                                                │
│   stg.*          silver.*        dw.*             etl.*        │
│   SalesRaw  ──►  SalesClean ──►  FactSales        EtlJob       │
│   (Bronze)       (Silver)        DimStore         EtlRun       │
│                                  DimProduct       EtlLog       │
│                                  DimCustomer      EtlSchedule  │
│                                  DimDate          auth.*       │
│                                                   UserAccount  │
└──────────────┬─────────────────────────────────────────────────┘
               │  Shared Entity Framework DbContext (ProjectContext)
       ┌───────┴──────────┐
       │                  │
┌──────▼──────┐    ┌──────▼───────────────────────────────┐
│ WPF Desktop │    │          ASP.NET Core Web            │
│ ETL_simulator│   │          ETL_web_project             │
│             │    │                                      │
│ • Генерує   │    │ • Dashboard — KPI, графіки           │
│   дані      │◄───│ • ETL Jobs / Logs / Staging          │
│ • Bronze    │    │ • Schedule — планувальник            │
│ • Silver    │    │ • Fact Explorer — аналітика          │
│ • Gold      │    │ • Admin — керування юзерами          │
│             │    │ • Settings — профіль                 │
└──────┬──────┘    └──────────────────────────────────────┘
       │ Named Pipe "etl-wpf-simulator"
       │ WPF слухає; Web пише "RUN"
       └─────────────────────────────────────────────────►
```

---

## Структура рішення

```
ETL-wpf-simulator-web-project/
│
├── ETL_simulator/                # WPF-застосунок (.NET 8, Windows)
│   ├── App.xaml / App.xaml.cs    # DI-container, запуск PipeListener
│   ├── PipeListener.cs           # Слухає Named Pipe, тригерує ETL
│   ├── ETL/
│   │   ├── BronzeLoader.cs       # Записує сирі дані у stg.SalesRaw
│   │   ├── SilverProcessor.cs    # Валідація + дедублікація → silver.SalesClean
│   │   ├── GoldLoader.cs         # Заповнює DimDate + FactSales у dw.*
│   │   ├── EtlOrchestrator.cs    # Керує EtlJob / EtlRun / EtlLog
│   │   └── EtlResult.cs          # Record-типи BronzeResult, SilverResult, GoldResult
│   ├── Generator/
│   │   ├── SalesGenerator.cs     # Bogus-генератор сирих продажів
│   │   ├── GeneratorConfig.cs    # Налаштування помилок / дублів / розміру батчу
│   │   └── PoolEntries.cs        # StoreEntry, ProductEntry, CustomerEntry
│   └── ViewModels/
│       ├── MainViewModel.cs      # MVVM-логіка: Start / Stop / Step / анімація
│       ├── LogEntry.cs           # Модель рядка логу
│       └── RelayCommand.cs       # ICommand-реалізація
│
├── ETL_web_project/              # ASP.NET Core MVC (.NET 8)
│   ├── Controllers/              # 6 контролерів
│   ├── Services/                 # 10 сервісів (реалізації)
│   ├── Interfaces/               # 10 інтерфейсів
│   ├── Data/
│   │   ├── Context/
│   │   │   └── ProjectContext.cs # EF Core DbContext — 13 DbSet'ів
│   │   └── Entities/             # 13 сутностей
│   ├── DTOs/                     # ~40 DTO-класів
│   ├── Enums/                    # EtlStatus, LogLevel, UserRole
│   ├── Handlers/
│   │   └── PasswordHashHandler.cs# PBKDF2-SHA512 (100k ітерацій)
│   ├── Mappings/
│   │   └── UserAccountProfile.cs # AutoMapper: RegisterDto → UserAccount (з хешуванням)
│   ├── Migrations/               # 13 EF Core міграцій
│   └── Views/                    # Razor-шаблони
│
├── ETL_simulator.Tests/          # xUnit — 57 тестів (WPF-компоненти)
│   ├── ETL/
│   │   ├── SilverProcessorTests.cs
│   │   └── EtlOrchestratorTests.cs
│   └── Generator/
│       └── SalesGeneratorTests.cs
│
├── ETL_web_project.Tests/        # xUnit — 60 тестів (Web-компоненти)
│   ├── Handlers/
│   │   └── PasswordHashHandlerTests.cs
│   └── Services/
│       ├── AccountServiceTests.cs
│       └── EtlScheduleServiceTests.cs
│
└── coverage-report/              # HTML-звіт покриття коду (index.html)
```

---

## Схема бази даних

### `stg` — Bronze (сирі дані)

| Колонка | Тип | Опис |
|---|---|---|
| `Id` | int PK | Авто-інкремент |
| `SalesTime` | datetime? | Час продажу (може бути null — навмисна помилка) |
| `StoreCode` | nvarchar(20)? | Код магазину (може бути null) |
| `ProductCode` | nvarchar(50)? | Код продукту (може бути null) |
| `CustomerCode` | nvarchar(10)? | Код клієнта (15% записів — null) |
| `Quantity` | int? | Кількість (може бути від'ємна) |
| `UnitPrice` | decimal? | Ціна за одиницю (може бути 0 або null) |
| `LoadedAt` | datetime | Час завантаження |
| `IsProcessedToSilver` | bit | Флаг обробки |

### `silver` — Silver (валідовані дані)

| Колонка | Тип | Опис |
|---|---|---|
| `Id` | bigint PK | |
| `SourceId` | int | FK → `stg.SalesRaw.Id` |
| `SalesTime` | datetime | Гарантовано не null |
| `StoreCode` | nvarchar(20) | Гарантовано не null |
| `ProductCode` | nvarchar(50) | Гарантовано не null |
| `CustomerCode` | nvarchar(10)? | Може бути null |
| `Quantity` | int | > 0 |
| `UnitPrice` | decimal | > 0 |
| `TotalAmount` | decimal | Quantity × UnitPrice |
| `CleanedAt` | datetime | Час очищення |
| `IsProcessedToGold` | bit | Флаг завантаження в Gold |

### `dw` — Gold (зірчаста схема)

```
DimDate ──┐
DimStore ─┼──► FactSales ◄── DimProduct
DimCustomer──┘
```

| Таблиця | Ключові поля |
|---|---|
| `DimDate` | DateKey, Date, Year, Month, Day, MonthName, DayOfWeek |
| `DimStore` | StoreKey, StoreCode, StoreName, City, Country, IsActive |
| `DimProduct` | ProductKey, ProductCode, ProductName, Category, UnitPrice, IsActive |
| `DimCustomer` | CustomerKey, CustomerCode, FullName, Gender, BirthDate, City |
| `FactSales` | SaleId, DateKey, StoreKey, ProductKey, CustomerKey?, Quantity, TotalAmount, CreatedAt |

### `etl` — Метадані

| Таблиця | Призначення |
|---|---|
| `EtlJob` | Реєстр ETL-завдань (код `SIMULATOR`) |
| `EtlRun` | Історія запусків (статус, час, рядки) |
| `EtlLog` | Детальний лог кожного запуску |
| `EtlSchedule` | Розклад запусків (FrequencyText, IsActive) |

### `auth` — Автентифікація

| Колонка | Опис |
|---|---|
| `Username` | Унікальний (індекс) |
| `Email` | Унікальний (індекс) |
| `PasswordHash` | PBKDF2-SHA512, base64 |
| `Role` | Analyst / DataEngineer / Admin |
| `IsActive` | Чи може увійти |
| `ResetToken` / `ResetTokenExpires` | Токен скидання пароля (1 год) |

---

## ETL-пайплайн — детально

### Крок 1 — Bronze (завантаження)

`BronzeLoader.LoadAsync(batch)` — просто зберігає список `SalesRaw` у `stg.SalesRaw` через `AddRange` + `SaveChanges`. Жодної валідації. Повертає `BronzeResult(Inserted)`.

### Крок 2 — Silver (очищення)

`SilverProcessor.ProcessAsync()` обробляє всі записи з `IsProcessedToSilver = false`:

```
Для кожного запису:
  1. Встановити IsProcessedToSilver = true
  2. Перевірити:
     ├─ SalesTime == null      → відхилити "Відсутня дата"       → nullDate++
     ├─ StoreCode == null      → відхилити "Null StoreCode"      → nullStore++
     ├─ ProductCode == null    → відхилити "Null ProductCode"    → nullProduct++
     ├─ Quantity <= 0 | null   → відхилити "Кількість ≤ 0"      → badQty++
     └─ UnitPrice <= 0 | null  → відхилити "Ціна ≤ 0"          → badPrice++
  3. Дедублікація (батч):
     key = (SalesTime, StoreCode, ProductCode)
     if seen.Contains(key) → відхилити "Дублікат (батч)"        → duplicates++
  4. Дедублікація (БД):
     SalesCleans.AnyAsync(key) → відхилити "Дублікат (БД)"      → duplicates++
  5. Вставити у silver.SalesClean (TotalAmount = Qty × Price)
```

Повертає `SilverResult(Inserted, Rejected, Duplicates, NullStore, NullProduct, BadQty, BadPrice, NullDate, RejectedReasons)`.

### Крок 3 — Gold (завантаження у сховище)

`GoldLoader.SeedDimensionsAsync()` — при першому запуску заповнює `DimStore`, `DimProduct`, `DimCustomer` зі статичних колекцій генератора.

`GoldLoader.LoadAsync()` — для кожного запису `silver.SalesClean` де `IsProcessedToGold = false`:
1. Знаходить `DimStore` і `DimProduct` за кодом
2. Знаходить або створює `DimDate` за датою продажу
3. Опційно знаходить `DimCustomer`
4. Вставляє `FactSales`
5. Ставить `IsProcessedToGold = true`

### Оркестрація (EtlOrchestrator)

На кожен крок:
1. `StartRunAsync(rowsRead)` — створює `EtlRun` зі статусом `Running`
2. `LogAsync(runId, level, msg)` — записує рядок у `EtlLog`
3. `FinishRunAsync(run, rowsInserted, status)` — закриває run (Success / Failed)

Якщо `EtlJob` з кодом `SIMULATOR` не існує — `EnsureJobAsync()` створює його автоматично.

---

## WPF-симулятор

### Запуск і DI

`App.xaml.cs` будує `ServiceCollection` з `appsettings.json`, реєструє всі ETL-компоненти як `Transient`, `GeneratorConfig` / `MainViewModel` як `Singleton`. Одразу стартує `PipeListener`.

### GeneratorConfig — параметри якості

| Параметр | За замовчуванням | Призначення |
|---|---|---|
| `BatchSize` | 10 | Кількість записів за один крок |
| `NullStoreRate` | 0.07 (7%) | Ймовірність null StoreCode |
| `NullProductRate` | 0.07 (7%) | Ймовірність null ProductCode |
| `NegativeQuantityRate` | 0.05 (5%) | Ймовірність від'ємної кількості |
| `DuplicateRate` | 0.05 (5%) | Ймовірність повтору попереднього запису |

Коли користувач рухає повзунок `ErrorRate` (0–100 %):
```csharp
NullStoreRate        = rate * 0.4;
NullProductRate      = rate * 0.4;
NegativeQuantityRate = rate * 0.2;
```

### Статичні пули даних (SalesGenerator)

| Пул | Розмір | Формат коду |
|---|---|---|
| Stores | 10 | `S001`–`S010` |
| Products | 100 | `P001`–`P100` |
| Customers | 250 | `C0001`–`C0250` |

Всі генеруються один раз у static-конструкторі через бібліотеку **Bogus**.

### Команди MainViewModel

| Команда | Умова | Дія |
|---|---|---|
| `Start` | `!IsRunning` | Запускає нескінченний `RunLoopAsync` у фоні |
| `Stop` | `IsRunning` | Скасовує CancellationToken, скидає статуси |
| `Step` | `!IsRunning` | Виконує один `RunStepAsync` вручну |
| `ClearLog` | завжди | Очищує список `Logs` |

Після кожного кроку — `AnimateBatchAsync`: покадровий показ кожного запису (passed / rejected) у UI з затримкою `DelayMs / BatchSize` мс.

---

## Веб-додаток

### Автентифікація

Cookie-автентифікація (ASP.NET Core). Після входу зберігає три Claims:
- `ClaimTypes.Name` — Username
- `ClaimTypes.Role` — роль
- `ClaimTypes.NameIdentifier` — UserId

**Захист паролю:** PBKDF2-HMACSHA512, 100 000 ітерацій, 128-бітна сіль, 256-бітний ключ. Хеш зберігається у base64.

### Маршрути та контролери

| Контролер | Маршрут | Ролі | Функціональність |
|---|---|---|---|
| `AccountController` | `/Account/*` | AllowAnonymous | Login, Register, ForgotPassword, ResetPassword, Logout |
| `DashboardController` | `/Dashboard/Index` | Всі авторизовані | Головна сторінка — KPI, графіки, топ-магазини/продукти |
| `EtlController` | `/Etl/*` | Admin, DataEngineer / Analyst | Jobs, Logs, JobRuns, Staging, Facts, RunJob |
| `EtlScheduleController` | `/EtlSchedule/*` | Admin, DataEngineer | Список розкладів, Create, Edit, ToggleActive, RunNow |
| `AdminController` | `/Admin/*` | Admin | Список користувачів, зміна ролі (з перевіркою пароля), блокування |
| `SettingsController` | `/Settings/*` | Всі авторизовані | Профіль (FullName, Email), зміна пароля |

### Dashboard — що відображається

- **KPI:** загальна сума продажів, кількість, сьогодні vs вчора (%)
- **Data Freshness:** коли востаннє завантажено в FactSales та Staging
- **ETL Run:** статус останнього запуску, тривалість, кількість невдалих за 24 год
- **Графік:** сума продажів за 7 / 14 / 30 днів
- **Топ-5 магазинів** і **Топ-5 продуктів** за сумою

### Staging (перегляд сирих даних)

- Таблиця `stg.SalesRaw` з фільтрацією
- Кнопка **Export CSV** — вивантажує весь staging у CSV
- Кнопка **Clear Staging** — очищує таблицю `stg.SalesRaw`

### Fact Explorer

- Перегляд таблиці `FactSales` із фільтрами: dateFrom/dateTo, store, product, customer
- Доступний ролям **Admin** та **Analyst**

### EtlSchedule — планувальник

- Зберігає текстовий опис розкладу (`FrequencyText`) і прив'язку до `EtlJob`
- **Toggle Active** — вмикає / вимикає розклад без видалення
- **Run Now** — одразу створює `EtlRun` зі статусом `Running → Success`

---

## Ролі користувачів

| Роль | Реєстрація | Dashboard | ETL Jobs/Logs/Staging | Fact Explorer | Schedule | Admin |
|---|---|---|---|---|---|---|
| **Analyst** | за замовчуванням | ✅ | ❌ | ✅ | ❌ | ❌ |
| **DataEngineer** | Admin змінює | ✅ | ✅ | ❌ | ✅ | ❌ |
| **Admin** | Admin змінює | ✅ | ✅ | ✅ | ✅ | ✅ |

> **Зміна ролі** в Admin-панелі вимагає підтвердження поточного пароля адміна.

---

## Зв'язок між проєктами (Named Pipe)

```
Web (EtlJobService)                WPF (PipeListener)
       │                                  │
       │  NamedPipeClientStream           │
       │  pipeName = "etl-wpf-simulator" │
       │  timeout  = 2 000 ms            │
       │──────── "RUN\n" ───────────────►│
       │                                  │ Dispatcher.Invoke(TriggerExternalRun)
       │                                  │──► RunStepAsync()
```

Якщо WPF-додаток не запущений — `ConnectAsync(2000)` кидає `TimeoutException`, веб показує повідомлення **"WPF Simulator не запущено."**

---

## Юніт-тести

```bash
dotnet test
```

| Проєкт | Тести | Стек |
|---|---|---|
| `ETL_simulator.Tests` | 57 | xUnit + EF InMemory |
| `ETL_web_project.Tests` | 60 | xUnit + EF InMemory + AutoMapper |
| **Разом** | **117** | **всі проходять** |

**Покриті компоненти:**

| Клас | Що тестується |
|---|---|
| `SilverProcessor` | null-поля, від'ємні значення, дублікати в батчі та БД, TotalAmount, позначення IsProcessedToSilver |
| `EtlOrchestrator` | StartRun/FinishRun, авто-створення Job, LogAsync, статуси |
| `SalesGenerator` | статичні пули, Generate() з rate=0/1, GenerateBatch() |
| `PasswordHashHandler` | PBKDF2 roundtrip, сіль унікальна, Unicode, спецсимволи, malformed hash |
| `AccountService` | Login, Register, UsernameExists, EmailExists, ResetToken, ResetPassword |
| `EtlScheduleService` | CRUD, ToggleActive, RunNow, подвійний toggle |

HTML-звіт покриття: `coverage-report/index.html`

---

## Запуск з нуля

### Вимоги

| Інструмент | Версія |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server | 2019+ / SQL Server 2022 / Azure SQL |
| Visual Studio | 2022+ (або Rider) |
| Windows | 10/11 (WPF вимагає Windows) |

### Крок 1 — Клонування репозиторію

```bash
git clone https://github.com/MrTomkaYurii/ETL-wpf-simulator-web-project.git
cd ETL-wpf-simulator-web-project
```

### Крок 2 — Налаштування рядка підключення

Відредагуйте **обидва** файли — вони мають вказувати на одну БД:

**`ETL_web_project/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost,1434;Initial Catalog=EtlDb;User ID=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

**`ETL_simulator/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost,1434;Initial Catalog=EtlDb;User ID=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

> **Порт 1434** — стандартний для SQL Server у Docker (якщо локальний екземпляр — замініть на `localhost` або `.\SQLEXPRESS`).

#### Варіант: SQL Server у Docker

```bash
docker run -e "ACCEPT_EULA=Y" \
           -e "SA_PASSWORD=yourStrong(!)Password" \
           -p 1434:1433 \
           --name etl-sql \
           -d mcr.microsoft.com/mssql/server:2022-latest
```

### Крок 3 — Застосування міграцій (створення БД)

```bash
cd ETL_web_project
dotnet ef database update
```

Це створить БД `EtlDb` і всі таблиці у схемах `stg`, `silver`, `dw`, `etl`, `auth`.

> Якщо `dotnet ef` не встановлено:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### Крок 4 — Створення першого адміністратора

Після застосування міграцій у БД немає жодного користувача. Зареєструйте першого через веб-інтерфейс, а потім вручну підвищте роль до `Admin`:

```sql
USE EtlDb;
UPDATE auth.UserAccount
SET Role = 2          -- 0=Analyst, 1=DataEngineer, 2=Admin
WHERE Username = 'ваш_логін';
```

### Крок 5 — Запуск веб-додатку

```bash
cd ETL_web_project
dotnet run
```

Додаток відкриється за адресою:
- HTTP: `http://localhost:5195`
- HTTPS: `https://localhost:7180`

### Крок 6 — Запуск WPF-симулятора

```bash
cd ETL_simulator
dotnet run
```

> **Порядок важливий:** кнопка **"Run Job"** у веб-інтерфейсі спрацює лише якщо WPF запущений.

### Крок 7 — Запуск юніт-тестів

```bash
dotnet test
```

Очікуваний результат:
```
Passed!  - Failed: 0, Passed: 57,  Total: 57  - ETL_simulator.Tests
Passed!  - Failed: 0, Passed: 60,  Total: 60  - ETL_web_project.Tests
```

---

### Типові помилки при першому запуску

| Помилка | Причина | Рішення |
|---|---|---|
| `A network-related error...` | SQL Server не запущено або неправильний рядок підключення | Перевірте сервер і `appsettings.json` |
| `Login failed for user 'sa'` | Неправильний пароль або SA не увімкнено | Увімкніть SQL Server Authentication у SSMS |
| `WPF Simulator не запущено` | Натиснули "Run Job" у веб, але WPF не запущений | Запустіть `ETL_simulator` |
| `Table 'auth.UserAccount' doesn't exist` | Міграції не застосовано | Виконайте `dotnet ef database update` |
| Порожній Dashboard | ETL ще не запускався | Запустіть крок у WPF-симуляторі |

---

### Технологічний стек

| Шар | Технологія |
|---|---|
| ORM | Entity Framework Core 8 (Code-First, Migrations) |
| БД | SQL Server 2019+ |
| Web Framework | ASP.NET Core 8 MVC |
| Автентифікація | Cookie Authentication + PBKDF2-SHA512 |
| Маппінг | AutoMapper 12 |
| WPF | .NET 8 WPF + MVVM (INotifyPropertyChanged, RelayCommand) |
| Генерація даних | Bogus 35 |
| IPC | Named Pipes (`System.IO.Pipes`) |
| Тести | xUnit 2.9 + EF Core InMemory |
| Покриття | coverlet.collector + ReportGenerator |
