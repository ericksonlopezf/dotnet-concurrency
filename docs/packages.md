# Package Reference & Compatibility Matrix

Complete reference for all **13 NuGet packages** published by `EricksonLopez.Concurrency`.

---

## 1. Package Matrix

| Package | NuGet ID | Layer | Target Frameworks | Direct Dependencies |
|---|---|---|---|---|
| [Abstractions](#abstractions) | `EricksonLopez.Concurrency.Abstractions` | Abstractions | `net8.0`, `net9.0`, `net10.0` | *(none)* |
| [Core](#core) | `EricksonLopez.Concurrency` | Core | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `OpenTelemetry.Api` |
| [Testing](#testing) | `EricksonLopez.Concurrency.Testing` | Testing | `net8.0`, `net9.0`, `net10.0` | `Abstractions` |
| [AspNetCore](#aspnetcore) | `EricksonLopez.Concurrency.AspNetCore` | Web Integration | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Microsoft.AspNetCore.App` (framework ref) |
| [Result](#result) | `EricksonLopez.Concurrency.Result` | Ecosystem Integration | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Core`, `EricksonLopez.Result` |
| [Mediator](#mediator) | `EricksonLopez.Concurrency.Mediator` | Ecosystem Integration | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Core`, `EricksonLopez.Mediator` |
| [Dapper](#dapper) | `EricksonLopez.Concurrency.Dapper` | Infrastructure | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Core`, `Dapper` |
| [PostgreSql](#postgresql) | `EricksonLopez.Concurrency.PostgreSql` | Database Dialect | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Core`, `Npgsql`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| [SqlServer](#sqlserver) | `EricksonLopez.Concurrency.SqlServer` | Database Dialect | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Microsoft.Data.SqlClient` |
| [MySql](#mysql) | `EricksonLopez.Concurrency.MySql` | Database Dialect | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `MySqlConnector` |
| [MariaDb](#mariadb) | `EricksonLopez.Concurrency.MariaDb` | Database Dialect | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `MySqlConnector` |
| [Oracle](#oracle) | `EricksonLopez.Concurrency.Oracle` | Database Dialect | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Oracle.ManagedDataAccess.Core` |
| [Sqlite](#sqlite) | `EricksonLopez.Concurrency.Sqlite` | Database Dialect | `net8.0`, `net9.0`, `net10.0` | `Abstractions`, `Microsoft.Data.Sqlite` |

---

## 2. Package Descriptions

### Abstractions

**NuGet ID**: `EricksonLopez.Concurrency.Abstractions`  
**Layer**: Abstractions (no external dependencies)  
**Primary Namespace**: `EricksonLopez.Concurrency.Abstractions`

The foundation package. Contains:
- Zero-allocation value types: `ConcurrencyVersion`, `ConcurrencyVersion<TEntity>`, `ExpectedVersion`, `ActualVersion`, `ConcurrencyToken`
- Core interfaces: `IVersionedEntity`, `IVersionedEntity<T>`, `IConcurrencyAware`, `IConcurrencyChecker`, `IConcurrencyController`, `IConcurrencyConflictResolver<TEntity>`, `IConcurrencyToken`
- Conflict models: `ConcurrencyConflict`, `ConcurrencyConflictType`, `ConcurrencyConflictClassification`, `ConflictResolutionStrategy`, `ConflictResolution<T>`, `CasResult<T>`
- Exceptions: `ConcurrencyException`, `ConcurrencyConfigurationException`, `ConcurrencyTokenMismatchException`

**Install**: `dotnet add package EricksonLopez.Concurrency.Abstractions`

---

### Core

**NuGet ID**: `EricksonLopez.Concurrency`  
**Layer**: Core implementation  
**Primary Namespaces**: `EricksonLopez.Concurrency.Controllers`, `.Diagnostics`, `.Resolvers`, `.DependencyInjection`

The main orchestration package. Contains:
- `ConcurrencyController`: Implements `IConcurrencyController` — `VerifyVersion`, `VerifyToken`, and atomic `ExecuteCasAsync<TEntity>`
- `OptimisticConcurrencyChecker`: Stateless singleton (`OptimisticConcurrencyChecker.Instance`) implementing `IConcurrencyChecker`
- Built-in resolvers: `RejectConflictResolver<T>`, `LastWriteWinsConflictResolver<T>`, `DelegateConflictResolver<T>`, `RefreshAndRetryConflictResolver<T>`
- `ConcurrencyOptions` and DI extension `AddEricksonLopezConcurrency()`
- `ConcurrencyDiagnostics`: OpenTelemetry `ActivitySource` and `Meter` (source name: `"EricksonLopez.Concurrency"`)

**Install**: `dotnet add package EricksonLopez.Concurrency`

---

### Testing

**NuGet ID**: `EricksonLopez.Concurrency.Testing`  
**Layer**: Testing (depends only on Abstractions)  
**Primary Namespace**: `EricksonLopez.Concurrency.Testing`

Mock-free testing utilities. Contains:
- `FakeConcurrencyController`: In-memory `IConcurrencyController` test double with canned outcome configuration, recorded invocations (`VerifyVersionInvocations`, `VerifyTokenInvocations`, `ExecuteCasInvocations`), and programmable custom delegates
- `ConcurrencyConflictBuilder`: Fluent builder for constructing `ConcurrencyConflict` instances in tests

**Install**: `dotnet add package EricksonLopez.Concurrency.Testing`

---

### AspNetCore

**NuGet ID**: `EricksonLopez.Concurrency.AspNetCore`  
**Layer**: Web Integration (depends only on Abstractions)  
**Primary Namespaces**: `EricksonLopez.Concurrency.AspNetCore.Middleware`, `.Models`, `.Extensions`

ASP.NET Core integration. Contains:
- `ConcurrencyConflictMiddleware`: Catches unhandled `ConcurrencyException` and returns RFC 7807 HTTP 409 Conflict responses
- `ConcurrencyProblemDetails`: RFC 7807 `ProblemDetails` model with conflict metadata
- Minimal API extensions: `Results.Extensions.ConcurrencyConflict(conflict)`
- HTTP header helpers: `GetExpectedConcurrencyToken`, `GetExpectedConcurrencyVersion`, `SetConcurrencyETag`
- DI extension: `services.AddConcurrencyAspNetCore()`
- Middleware registration: `app.UseConcurrencyConflictHandling()`

> **Design note** (ADR-010): `AspNetCore` references only `Abstractions` — it introduces zero transitive dependency on the Core package, keeping the web integration lightweight.

**Install**: `dotnet add package EricksonLopez.Concurrency.AspNetCore`

---

### Result

**NuGet ID**: `EricksonLopez.Concurrency.Result`  
**Layer**: Ecosystem Integration  
**Primary Namespace**: `EricksonLopez.Concurrency.Result`

Monadic integration with `EricksonLopez.Result`. Contains:
- `ConcurrencyResultExtensions`: Extension methods converting `CasResult<T>`, `ConcurrencyConflict?`, and `ConflictResolution<T>` to `Result<T>`
- `ConcurrencyErrors`: Typed `Error` factory mapping conflict details into structured error codes

**Install**: `dotnet add package EricksonLopez.Concurrency.Result`

---

### Mediator

**NuGet ID**: `EricksonLopez.Concurrency.Mediator`  
**Layer**: Ecosystem Integration  
**Primary Namespace**: `EricksonLopez.Concurrency.Mediator`

CQRS pipeline integration with `EricksonLopez.Mediator`. Contains:
- `IConcurrencyAwareRequest`: Marker interface for commands with concurrency constraints
- `IConcurrencyAwareRequest<TResponse>`: Typed contract for `ExpectedVersion`-aware CQRS commands
- `ConcurrencyBehavior<TRequest, TResponse>`: Observability pipeline behavior (`sealed class`) creating OpenTelemetry spans and recording conflict metrics (observability-only — version enforcement must occur at handler boundary, per ADR-005)
- DI extension: `services.AddConcurrencyMediatorBehavior()`

**Install**: `dotnet add package EricksonLopez.Concurrency.Mediator`

---

### Dapper

**NuGet ID**: `EricksonLopez.Concurrency.Dapper`  
**Layer**: Infrastructure  
**Primary Namespace**: `EricksonLopez.Concurrency.Dapper`

Zero-roundtrip Dapper integration. Contains:
- `ConcurrencyDapperExtensions.ExecuteOptimisticAsync`: Executes parameterized SQL and detects 0 rows affected as `ConcurrencyConflict.VersionMismatch`
- `ConcurrencyDapperExtensions.ExecuteOptimisticTokenAsync`: Token-based variant
- `OptimisticUpdateBuilder.BuildVersionedUpdate`: Generates `UPDATE ... SET version = version + 1 WHERE id = @Id AND version = @ExpectedVersion` SQL with optional multi-tenancy (`tenant_id`) clause
- DI extension: `services.AddEricksonLopezConcurrencyDapper()`

**Install**: `dotnet add package EricksonLopez.Concurrency.Dapper`

---

### PostgreSql

**NuGet ID**: `EricksonLopez.Concurrency.PostgreSql`  
**Layer**: Database Dialect  
**Primary Namespace**: `EricksonLopez.Concurrency.PostgreSql`

PostgreSQL adapter. Contains:
- `XminConcurrencyToken`: `IConcurrencyToken` wrapping the 32-bit system `xmin` transaction ID column
- `PostgreSqlConcurrencyErrorClassifier`: SQLSTATE classifier for `40001` (serialization failure), `40P01` (deadlock), `55P03` (lock unavailable), `23505` (unique violation)
- `PostgreSqlLockExtensions.WithLock(mode)`: Appends `FOR UPDATE`, `FOR UPDATE NOWAIT`, `FOR UPDATE SKIP LOCKED`, `FOR SHARE`, `FOR NO KEY UPDATE` clauses
- DI extension: `services.AddEricksonLopezConcurrencyPostgreSql()`

**Install**: `dotnet add package EricksonLopez.Concurrency.PostgreSql`

---

### SqlServer

**NuGet ID**: `EricksonLopez.Concurrency.SqlServer`  
**Layer**: Database Dialect (depends only on Abstractions)  
**Primary Namespace**: `EricksonLopez.Concurrency.SqlServer`

SQL Server adapter. Contains:
- `SqlServerRowVersionToken`: `IConcurrencyToken` wrapping the 8-byte binary `ROWVERSION` / `TIMESTAMP` column
- `SqlServerErrorClassifier`: Classifies `SqlException` error numbers: `1205` (deadlock), `3960`/`3961` (snapshot conflict), `1222` (lock timeout), `2601`/`2627` (unique constraint violations)
- `SqlServerLockExtensions.WithSqlServerTableHint(mode)`: Appends table hints (`WITH (UPDLOCK, ROWLOCK)`, `WITH (XLOCK, ROWLOCK, NOWAIT)`, `WITH (ROWLOCK, READPAST)`)
- DI extension: `services.AddEricksonLopezConcurrencySqlServer()`

**Install**: `dotnet add package EricksonLopez.Concurrency.SqlServer`

---

### MySql

**NuGet ID**: `EricksonLopez.Concurrency.MySql`  
**Layer**: Database Dialect (depends only on Abstractions)  
**Primary Namespace**: `EricksonLopez.Concurrency.MySql`

MySQL adapter using `MySqlConnector`. Contains:
- `MySqlConcurrencyErrorClassifier`: Classifies MySQL error codes: `1213` (deadlock), `1205` (lock wait timeout), `1062` (duplicate key / unique constraint)
- `MySqlLockExtensions.WithMySqlLock(mode)`: Appends `FOR UPDATE`, `FOR SHARE`, `FOR UPDATE NOWAIT`, `FOR UPDATE SKIP LOCKED`
- DI extension: `services.AddEricksonLopezConcurrencyMySql()`

**Install**: `dotnet add package EricksonLopez.Concurrency.MySql`

---

### MariaDb

**NuGet ID**: `EricksonLopez.Concurrency.MariaDb`  
**Layer**: Database Dialect (depends only on Abstractions)  
**Primary Namespace**: `EricksonLopez.Concurrency.MariaDb`

MariaDB adapter using `MySqlConnector`. Contains:
- `MariaDbConcurrencyErrorClassifier`: Classifies MariaDB error codes: `1213` (deadlock), `1205` (lock wait timeout), `1062` (duplicate key)
- `MariaDbLockExtensions.WithMariaDbLock(mode)`: Appends `FOR UPDATE`, `LOCK IN SHARE MODE`
- `MariaDbLockExtensions.WithMariaDbLockWait(seconds)`: Appends timed `FOR UPDATE WAIT n`
- DI extension: `services.AddEricksonLopezConcurrencyMariaDb()`

**Install**: `dotnet add package EricksonLopez.Concurrency.MariaDb`

---

### Oracle

**NuGet ID**: `EricksonLopez.Concurrency.Oracle`  
**Layer**: Database Dialect (depends only on Abstractions)  
**Primary Namespace**: `EricksonLopez.Concurrency.Oracle`

Oracle adapter. Contains:
- `OracleRowScnToken`: `IConcurrencyToken` wrapping the 64-bit `ORA_ROWSCN` pseudo-column (System Change Number)
- `OracleConcurrencyErrorClassifier`: Classifies Oracle error codes: `ORA-00060` (deadlock), `ORA-00054` (resource busy), `ORA-08177` (serialization failure), `ORA-00001` (unique constraint)
- `OracleLockExtensions.WithOracleLock(mode)`: Appends `FOR UPDATE`, `FOR UPDATE NOWAIT`, `FOR UPDATE SKIP LOCKED`
- `OracleLockExtensions.WithOracleLockWait(seconds)`: Appends timed `FOR UPDATE WAIT n`
- DI extension: `services.AddEricksonLopezConcurrencyOracle()`

**Install**: `dotnet add package EricksonLopez.Concurrency.Oracle`

---

### Sqlite

**NuGet ID**: `EricksonLopez.Concurrency.Sqlite`  
**Layer**: Database Dialect (depends only on Abstractions)  
**Primary Namespace**: `EricksonLopez.Concurrency.Sqlite`

SQLite adapter. Contains:
- `SqliteConcurrencyErrorClassifier`: Classifies SQLite result codes: `SQLITE_BUSY` (5), `SQLITE_LOCKED` (6), `SQLITE_CONSTRAINT` (19)
- DI extension: `services.AddEricksonLopezConcurrencySqlite()`

> **Note**: SQLite does not have native `xmin`-equivalent tokens or advanced locking hints. It relies on transaction isolation levels and its built-in locking protocol.

**Install**: `dotnet add package EricksonLopez.Concurrency.Sqlite`

---

## 3. Installation Selection Guide

| Use Case | Required Packages |
|---|---|
| Domain versioning only (in-memory CAS) | `EricksonLopez.Concurrency` |
| Dapper zero-roundtrip writes (PostgreSQL) | `EricksonLopez.Concurrency`, `EricksonLopez.Concurrency.Dapper`, `EricksonLopez.Concurrency.PostgreSql` |
| ASP.NET Core REST API with ETags | `EricksonLopez.Concurrency`, `EricksonLopez.Concurrency.AspNetCore` |
| CQRS pipeline with Result monad | `EricksonLopez.Concurrency`, `EricksonLopez.Concurrency.Mediator`, `EricksonLopez.Concurrency.Result` |
| Full enterprise stack (all integrations, PostgreSQL) | `EricksonLopez.Concurrency`, `EricksonLopez.Concurrency.Dapper`, `EricksonLopez.Concurrency.PostgreSql`, `EricksonLopez.Concurrency.Mediator`, `EricksonLopez.Concurrency.Result`, `EricksonLopez.Concurrency.AspNetCore` |
| Unit testing handlers with mocks | `EricksonLopez.Concurrency.Testing` |
| Multi-database targeting (SQL Server + SQLite) | `EricksonLopez.Concurrency.SqlServer`, `EricksonLopez.Concurrency.Sqlite` |

---

## 4. Version Conventions

All packages in the `EricksonLopez.Concurrency` ecosystem are versioned together using a single version number. All 13 packages in each release share the same `<Version>` as defined in `Directory.Build.props`.

Current version: See [CHANGELOG.md](../CHANGELOG.md) or the NuGet badge in [README.md](../README.md).

Version increments follow [Semantic Versioning](https://semver.org/):
- **Patch**: Bug fixes and classifier adjustments.
- **Minor**: New packages, new API additions (backward compatible).
- **Major**: Breaking changes to `Abstractions` (interfaces, value type contracts).
