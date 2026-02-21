# 05. Данные

## СУБД

**Microsoft SQL Server 2022** (Docker-образ `mcr.microsoft.com/mssql/server:2022-latest`).

Доступ через EF Core 8. ORM используется для всех операций (нет Dapper / raw SQL).

## Схема базы данных

```mermaid
erDiagram
    User ||--o{ UserRole : has
    User ||--o{ UserPermissionOverride : has
    User ||--o{ UserRefreshToken : has
    User ||--o{ DataScope : has
    Role ||--o{ UserRole : has
    Role ||--o{ RolePermission : has
    Permission ||--o{ RolePermission : has
    Permission ||--o{ UserPermissionOverride : has

    Client ||--o{ ClientAddress : has
    Client ||--o| InvestmentProfile : has
    Client }o--|| Country : "residence"
    Client }o--|| Country : "citizenship"
    ClientAddress }o--|| Country : "address country"

    Account ||--o{ AccountHolder : has
    Client ||--o{ AccountHolder : has
    Account }o--o| Clearer : "clearer"
    Account }o--o| TradePlatform : "platform"

    User {
        guid Id PK
        string Username UK
        string Email UK
        string PasswordHash
        string FullName
        bool IsActive
        binary RowVersion
        datetime CreatedAt
        string CreatedBy
    }

    Role {
        guid Id PK
        string Name UK
        string Description
        bool IsSystem
        binary RowVersion
        datetime CreatedAt
    }

    Permission {
        guid Id PK
        string Code UK
        string Name
        string Description
        string Group
    }

    Client {
        guid Id PK
        string Email UK
        enum ClientType
        enum ClientStatus
        enum KycStatus
        enum RiskLevel
        bool PepStatus
        string FirstName
        string LastName
        string CompanyName
        guid ResidenceCountryId FK
        guid CitizenshipCountryId FK
        binary RowVersion
        datetime CreatedAt
    }

    ClientAddress {
        guid Id PK
        guid ClientId FK
        enum AddressType
        string Line1
        string City
        guid CountryId FK
    }

    InvestmentProfile {
        guid Id PK
        guid ClientId UK
        enum Objective
        enum RiskTolerance
        enum TimeHorizon
        string Notes
    }

    Country {
        guid Id PK
        string Iso2 UK
        string Iso3 UK
        string Name
        string FlagEmoji
        bool IsActive
    }

    Account {
        guid Id PK
        string Number UK
        guid ClearerId FK
        guid TradePlatformId FK
        enum AccountStatus
        enum AccountType
        enum MarginType
        enum OptionLevel
        enum Tariff
        enum DeliveryType
        datetime OpenedAt
        datetime ClosedAt
        string Comment
        string ExternalId
        binary RowVersion
        datetime CreatedAt
    }

    AccountHolder {
        guid AccountId PK_FK
        guid ClientId PK_FK
        enum HolderRole PK
        bool IsPrimary
        datetime AddedAt
    }

    Clearer {
        guid Id PK
        string Name UK
        string Description
        bool IsActive
    }

    TradePlatform {
        guid Id PK
        string Name UK
        string Description
        bool IsActive
    }

    AuditLog {
        guid Id PK
        guid UserId IX
        string UserName
        string Action IX
        string EntityType IX
        string EntityId
        string BeforeJson
        string AfterJson
        string CorrelationId
        datetime CreatedAt IX
        int StatusCode
        bool IsSuccess
    }
```

## Ключевые сущности

### Identity & Access

| Таблица | Назначение | Уникальные индексы |
|---------|------------|-------------------|
| Users | Пользователи системы | Username, Email |
| Roles | Роли (в т.ч. системные) | Name |
| Permissions | Гранулярные права (18 шт.) | Code |
| UserRoles | Связь M:N User <-> Role | (UserId, RoleId) |
| RolePermissions | Связь M:N Role <-> Permission | (RoleId, PermissionId) |
| UserPermissionOverrides | Персональные переопределения прав | (UserId, PermissionId) |
| DataScopes | Области видимости данных (row-level) | (UserId, ScopeType, ScopeValue) |
| UserRefreshTokens | Refresh-токены с ротацией | TokenHash |

### Клиенты

| Таблица | Назначение | Особенности |
|---------|------------|-------------|
| Clients | Клиенты (Individual / Corporate) | Условные поля по ClientType |
| ClientAddresses | Адреса клиентов (Legal/Mailing/Working) | Cascade delete |
| InvestmentProfiles | Инвестиционный профиль (1:1 с Client) | Cascade delete |
| Countries | Справочник стран (ISO 3166) | Seed data |

### Счета

| Таблица | Назначение | Особенности |
|---------|------------|-------------|
| Accounts | Счета (Individual, Corporate, Joint, Trust, IRA) | RowVersion, FK на Clearer/TradePlatform |
| AccountHolders | Связь M:N Account <-> Client | Composite PK (AccountId, ClientId, Role), Cascade delete |
| Clearers | Справочник клиринговых компаний | Seed data |
| TradePlatforms | Справочник торговых платформ | Seed data |

### Аудит

| Таблица | Назначение | Особенности |
|---------|------------|-------------|
| AuditLogs | Лог всех мутаций | Индексы: CreatedAt, UserId, Action, EntityType |

## Перечисления (Enums)

Хранятся как `int` в БД:

| Enum | Значения |
|------|----------|
| ClientType | Individual (0), Corporate (1) |
| ClientStatus | Active (0), Blocked (1), PendingKyc (2) |
| KycStatus | NotStarted (0), InProgress (1), Approved (2), Rejected (3) |
| RiskLevel | Low (0), Medium (1), High (2) |
| Gender | Male (0), Female (1), Other (2), Unspecified (3) |
| AddressType | Legal (0), Mailing (1), Working (2) |
| InvestmentObjective | Preservation (0), Income (1), Growth (2), Speculation (3), Hedging (4), Other (5) |
| AccountStatus | Active (0), Blocked (1), Closed (2), Suspended (3) |
| AccountType | Individual (0), Corporate (1), Joint (2), Trust (3), IRA (4) |
| MarginType | Cash (0), RegT (1), Portfolio (2) |
| OptionLevel | Level0 (0), Level1 (1), Level2 (2), Level3 (3), Level4 (4) |
| Tariff | Basic (0), Standard (1), Premium (2), VIP (3) |
| DeliveryType | Paper (0), Electronic (1) |
| HolderRole | Owner (0), Beneficiary (1), Trustee (2), PowerOfAttorney (3), Custodian (4), Authorized (5) |

## Миграции

EF Core Code-First миграции:

| # | Имя | Описание |
|---|-----|----------|
| 1 | 20260217195408_IdentityAccess_Initial | Users, Roles, Permissions, RefreshTokens |
| 2 | 20260218151925_AddClients | Clients, ClientAddresses, InvestmentProfiles |
| 3 | 20260219100000_ExtendClientsAndCountries | Countries, связи с клиентами |
| 4 | 20260219120000_AddCountryFlagEmoji | Колонка FlagEmoji |
| 5 | 20260220211449_AddAccounts | Accounts, Clearers, TradePlatforms |
| 6 | 20260220213800_AddAccountHolders | AccountHolders (M:N Account <-> Client) |

Миграции применяются **автоматически** при старте приложения (`context.Database.MigrateAsync()`).

## Seed Data

При первом запуске засеиваются:

1. **Permissions** (18 прав) -- из массива `Permissions.All` в коде
2. **Countries** -- полный список стран с ISO-кодами и флагами
3. **Admin user** -- логин `admin`, пароль из переменной окружения `ADMIN_PASSWORD`
4. **Роль Administrator** -- системная роль со всеми permissions
5. **Clearers** -- справочник клиринговых компаний (Apex Clearing, Pershing, Interactive Brokers, Hilltop Securities)
6. **TradePlatforms** -- справочник торговых платформ (MetaTrader 5, Sterling Trader, DAS Trader, Thinkorswim)
7. **Demo data** (опционально, `SEED_DEMO_DATA=true`) -- тестовые пользователи, роли, клиенты, счета (150 шт.), холдеры

## Конкурентность и транзакции

- **Optimistic Concurrency:** все основные сущности используют `RowVersion` (SQL Server `rowversion`)
- **Явные транзакции:** `UpdateClientCommandHandler` использует `BeginTransactionAsync` для атомарного обновления клиента + адресов + инвестиционного профиля
- **Остальные операции:** неявная транзакция через `SaveChangesAsync`

## Ограничения и FK

- `ON DELETE CASCADE`: ClientAddresses, InvestmentProfiles (при удалении Client)
- `ON DELETE CASCADE`: AccountHolders (при удалении Account)
- `ON DELETE RESTRICT`: Clients -> Countries (нельзя удалить страну, если есть клиенты)
- `ON DELETE RESTRICT`: AccountHolders -> Clients (нельзя удалить клиента, если он холдер счёта)
- `ON DELETE SET NULL`: Accounts -> Clearers, Accounts -> TradePlatforms
- `ON DELETE CASCADE`: UserRoles, RolePermissions (при удалении User/Role)
