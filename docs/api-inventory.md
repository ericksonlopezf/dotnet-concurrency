# Public API Inventory: EricksonLopez.Concurrency

This document serves as the **Authoritative and Exhaustive Public API Inventory** for the **`EricksonLopez.Concurrency`** ecosystem across **.NET 8, .NET 9, and .NET 10**.

> **Single Source of Truth**:
> This inventory contains exclusively the types and contracts exported by production projects categorized as **Core Library** and **Infrastructure**. Every code example in the Showcase project, test suite, and technical documentation maps directly to these verified public APIs.

---

## 📊 Global Package & Type Matrix

| Project | Classification | Primary Namespace | Total Public Types |
|---|---|---|:---:|
| `EricksonLopez.Concurrency.Abstractions` | Core Library | `EricksonLopez.Concurrency.Abstractions` | 25 |
| `EricksonLopez.Concurrency` | Core Library | `EricksonLopez.Concurrency.Controllers`, `.Diagnostics`, `.Resolvers`, `.DependencyInjection` | 9 |
| `EricksonLopez.Concurrency.AspNetCore` | Infrastructure | `EricksonLopez.Concurrency.AspNetCore.Extensions`, `.Middleware`, `.Models`, `.DependencyInjection` | 7 |
| `EricksonLopez.Concurrency.Dapper` | Infrastructure | `EricksonLopez.Concurrency.Dapper`, `.DependencyInjection` | 3 |
| `EricksonLopez.Concurrency.MariaDb` | Infrastructure | `EricksonLopez.Concurrency.MariaDb`, `.DependencyInjection` | 4 |
| `EricksonLopez.Concurrency.Mediator` | Infrastructure | `EricksonLopez.Concurrency.Mediator`, `.DependencyInjection` | 4 |
| `EricksonLopez.Concurrency.MySql` | Infrastructure | `EricksonLopez.Concurrency.MySql`, `.DependencyInjection` | 4 |
| `EricksonLopez.Concurrency.Oracle` | Infrastructure | `EricksonLopez.Concurrency.Oracle`, `.DependencyInjection` | 5 |
| `EricksonLopez.Concurrency.PostgreSql` | Infrastructure | `EricksonLopez.Concurrency.PostgreSql`, `.DependencyInjection` | 5 |
| `EricksonLopez.Concurrency.Result` | Infrastructure | `EricksonLopez.Concurrency.Result` | 2 |
| `EricksonLopez.Concurrency.Sqlite` | Infrastructure | `EricksonLopez.Concurrency.Sqlite`, `.DependencyInjection` | 2 |
| `EricksonLopez.Concurrency.SqlServer` | Infrastructure | `EricksonLopez.Concurrency.SqlServer`, `.DependencyInjection` | 5 |
| `EricksonLopez.Concurrency.Testing` | Infrastructure | `EricksonLopez.Concurrency.Testing` | 5 |
| **Grand Total** | | | **80 Public Types** |

---

## 1. `EricksonLopez.Concurrency.Abstractions` (Core Library)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `ConcurrencyVersion` | `EricksonLopez.Concurrency.Abstractions` | Zero-allocation `readonly record struct` encapsulating a 64-bit monotonic integer with `ISpanParsable` and `ISpanFormattable` support. | None | Numeric entity and aggregate root versioning. | Basic | Yes (Levels 01, 03, 07, 11) |
| `ConcurrencyVersion<TEntity>` | `EricksonLopez.Concurrency.Abstractions` | Strongly typed generic record struct binding a version to a specific domain entity type. | `TEntity` | DDD domain modeling where strict compile-time entity type differentiation is required. | Intermediate | Yes (Level 03) |
| `ExpectedVersion` | `EricksonLopez.Concurrency.Abstractions` | Version precondition required prior to mutating an entity (`Any`, `New`, `Exists`, `Specific`). | `ConcurrencyVersion` | Command validation, entity insertion, and conditional updates. | Basic | Yes (Levels 01, 03, 04, 05, 10) |
| `ActualVersion` | `EricksonLopez.Concurrency.Abstractions` | Discovered state version found in persistence or cache (`From`, `NotFound`). | `ConcurrencyVersion` | Discovered state reporting following a concurrency collision. | Basic | Yes (Levels 03, 06, 10) |
| `ConcurrencyToken` | `EricksonLopez.Concurrency.Abstractions` | Immutable opaque token for state coherence validation (GUID, hash, or binary rowversion). | None | HTTP `ETag` and `If-Match` headers, distributed cache coherence. | Basic | Yes (Levels 03, 04, 10, 11) |
| `CasResult<TEntity>` | `EricksonLopez.Concurrency.Abstractions` | Outcome of a Compare-And-Swap operation (`IsSuccess`, `IsConflict`, `Entity`, `NewVersion`, `Conflict`). | `TEntity`, `ConcurrencyConflict`, `ConcurrencyVersion` | Atomic in-memory state transitions on aggregates or caches. | Intermediate | Yes (Levels 05, 11) |
| `CasResult` | `EricksonLopez.Concurrency.Abstractions` | Static factory methods for creating `CasResult<TEntity>.Succeeded` and `CasResult<TEntity>.Conflicted`. | `CasResult<TEntity>` | Syntactic convenience for returning CAS results. | Basic | Yes (Level 11) |
| `ConflictResolution<TEntity>` | `EricksonLopez.Concurrency.Abstractions` | Descriptor structure for conflict reconciliation outcome (`IsResolved`, `Strategy`, `ResolvedEntity`, `Reason`). | `TEntity`, `ConflictResolutionStrategy` | Output from an `IConcurrencyConflictResolver<TEntity>`. | Intermediate | Yes (Levels 08, 11) |
| `ConflictResolution` | `EricksonLopez.Concurrency.Abstractions` | Static factory methods for creating resolutions (`Rejected`, `Merged`, `LastWriteWins`, `RefreshedAndRetried`). | `ConflictResolution<TEntity>` | Fluent construction of conflict resolution outcomes. | Basic | Yes (Levels 08, 11) |
| `ConcurrencyConflict` | `EricksonLopez.Concurrency.Abstractions` | Immutable record containing full diagnostic conflict metadata (IDs, versions, tokens, classification, timestamp). | `ExpectedVersion`, `ActualVersion`, `IConcurrencyToken` | Telemetry, logging, RFC 7807 problem details serialization, and error mapping. | Basic | Yes (Levels 01, 03, 04, 06, 10, 11) |
| `IConcurrencyController` | `EricksonLopez.Concurrency.Abstractions` | Primary orchestrator contract for optimistic validation and atomic in-memory CAS mutations. | `IVersionedEntity`, `IConcurrencyAware`, `ExpectedVersion` | Dependency injection into application services and command handlers. | Intermediate | Yes (Levels 01, 02, 05, 10) |
| `IConcurrencyChecker` | `EricksonLopez.Concurrency.Abstractions` | Synchronous zero-allocation evaluation contract for version and token matching. | `ExpectedVersion`, `ConcurrencyVersion`, `IConcurrencyToken` | High-frequency evaluations in hot loops, pipelines, and filters. | Basic | Yes (Levels 02, 03, 07) |
| `IConcurrencyConflictResolver<TEntity>` | `EricksonLopez.Concurrency.Abstractions` | Asynchronous contract for conflict resolution strategies. | `TEntity`, `ConcurrencyConflict`, `ConflictResolution<TEntity>` | Custom automatic reconciliation (Merge, LWW, Retry). | Advanced | Yes (Levels 02, 08) |
| `IConcurrencyToken` | `EricksonLopez.Concurrency.Abstractions` | Common interface for opaque concurrency tokens with format discriminators. | None | Database dialect token polymorphism (xmin, ORA_ROWSCN, ROWVERSION). | Basic | Yes (Levels 03, 09, 11) |
| `IVersionedEntity` | `EricksonLopez.Concurrency.Abstractions` | Contract defining an entity with a monotonic 64-bit numeric `long Version` property. | None | Standard domain entities with numeric versioning. | Basic | Yes (Levels 01, 04, 08, 10) |
| `IVersionedEntity<TEntity>` | `EricksonLopez.Concurrency.Abstractions` | Contract defining an entity with strongly typed `ConcurrencyVersion<TEntity>`. | `TEntity`, `ConcurrencyVersion<TEntity>` | Domain models requiring compile-time type safety across entity versions. | Intermediate | Yes (Level 03) |
| `IMutableVersionedEntity` | `EricksonLopez.Concurrency.Abstractions` | Contract for mutable entities whose version can be updated in-place by concurrency controllers. | `IVersionedEntity` | Mutable aggregate models where the controller advances the version upon CAS success. | Intermediate | Yes (Levels 03, 05) |
| `IConcurrencyAware` | `EricksonLopez.Concurrency.Abstractions` | Contract for entities or DTOs encapsulating an `IConcurrencyToken`. | `IConcurrencyToken` | RESTful resources and entities using opaque tokens or ETags. | Basic | Yes (Levels 03, 04, 11) |
| `ConcurrencyConflictClassification` | `EricksonLopez.Concurrency.Abstractions` | Operational classification enum (`Transient`, `Retryable`, `NonRetryable`, `StaleState`, `Fatal`). | None | Resilience policies, automated retry decisions, and HTTP status codes. | Basic | Yes (Levels 01, 02, 06, 10) |
| `ConcurrencyConflictType` | `EricksonLopez.Concurrency.Abstractions` | Technical conflict nature enum (`VersionMismatch`, `TokenMismatch`, `StateDeleted`, `Deadlock`, etc.). | None | Telemetry, monitoring, and error root cause analysis. | Basic | Yes (Levels 01, 06, 10, 11) |
| `ConflictResolutionStrategy` | `EricksonLopez.Concurrency.Abstractions` | Conflict resolution action enum (`None`, `Reject`, `MergeDomainSpecific`, `LastWriteWinsExplicit`, `RefreshAndRetry`). | None | Configuring arbitration policies in `ConcurrencyOptions`. | Basic | Yes (Levels 02, 08, 11) |
| `ExpectedVersionKind` | `EricksonLopez.Concurrency.Abstractions` | Precondition discriminator enum (`Any`, `New`, `Exists`, `Specific`). | None | Internal precondition evaluation in `ExpectedVersion`. | Basic | Yes (Level 03) |
| `ConcurrencyException` | `EricksonLopez.Concurrency.Abstractions` | Specialized domain exception thrown when an unresolvable concurrency conflict occurs. | `ConcurrencyConflict` | Caught by HTTP middleware for automatic RFC 7807 (409) translation. | Intermediate | Yes (Levels 06, 10, 11) |
| `ConcurrencyTokenMismatchException` | `EricksonLopez.Concurrency.Abstractions` | Typed exception thrown upon token or ETag header mismatch. | `IConcurrencyToken` | Strict validation in HTTP transport layers or REST endpoints. | Intermediate | Yes (Level 06) |
| `ConcurrencyConfigurationException` | `EricksonLopez.Concurrency.Abstractions` | Exception thrown when an option or resolution strategy is invalidly configured. | None | Early configuration validation during DI container startup. | Basic | Yes (Level 06) |

---

## 2. `EricksonLopez.Concurrency` (Core)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `ConcurrencyController` | `EricksonLopez.Concurrency.Controllers` | Production implementation of `IConcurrencyController` with striped lock pooling, timeouts, and OpenTelemetry instrumentation. | `IConcurrencyChecker`, `ConcurrencyOptions` | In-memory verification, CAS mutual exclusion, and state synchronization in domain services. | Intermediate | Yes (Levels 01, 02, 05, 10) |
| `OptimisticConcurrencyChecker` | `EricksonLopez.Concurrency.Controllers` | Thread-safe, zero-allocation singleton implementation of `IConcurrencyChecker`. | None | Ultra-fast in-memory version and token evaluations (~1.14 ns). | Basic | Yes (Levels 02, 03, 07) |
| `ConcurrencyOptions` | `EricksonLopez.Concurrency.DependencyInjection` | Configuration options model (`DefaultResolutionStrategy`, `DefaultMaxAcquisitionTimeout`, `DefaultMaxExecutionTimeout`, etc.). | `ConflictResolutionStrategy` | Customizing global engine behavior, lock timeouts, and diagnostics. | Basic | Yes (Level 02) |
| `ConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.DependencyInjection` | Extension methods for `IServiceCollection` (`AddEricksonLopezConcurrency`, `AddConflictResolver`). | `IServiceCollection` | Dependency injection configuration in application composition roots. | Basic | Yes (Levels 01, 02, 10) |
| `ConcurrencyDiagnostics` | `EricksonLopez.Concurrency.Diagnostics` | Provides `ActivitySource`, `Meter`, histograms, and counters for OpenTelemetry distributed tracing. | `System.Diagnostics` | Emitting conflict rates, durations, and resolution outcomes. | Intermediate | Yes (Levels 07, 11) |
| `RejectConflictResolver<TEntity>` | `EricksonLopez.Concurrency.Resolvers` | Default resolver that rejects all conflicts strictly. | `IConcurrencyConflictResolver<TEntity>` | Default strategy for systems requiring strict consistency. | Basic | Yes (Level 08) |
| `LastWriteWinsConflictResolver<TEntity>` | `EricksonLopez.Concurrency.Resolvers` | Explicit resolver that overwrites persisted state with the incoming proposed version. | `IConcurrencyConflictResolver<TEntity>` | Specific scenarios where latest authorized write must prevail. | Intermediate | Yes (Level 08) |
| `DelegateConflictResolver<TEntity>` | `EricksonLopez.Concurrency.Resolvers` | Resolver delegating reconciliation to a custom lambda or domain delegate. | `Func<TEntity, TEntity?, ConcurrencyConflict, ...>` | Custom domain merge logic (e.g., cumulative balances or additive changes). | Advanced | Yes (Level 08) |
| `RefreshAndRetryConflictResolver<TEntity>` | `EricksonLopez.Concurrency.Resolvers` | Resolver that reloads the latest state from persistence and reapplies the domain mutation. | `Func<string, ...>`, `Func<TEntity, TEntity, ...>` | Contention mitigation where re-fetching state allows clean reapplication. | Advanced | Yes (Level 08) |

---

## 3. `EricksonLopez.Concurrency.AspNetCore` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `ConcurrencyAspNetCoreServiceCollectionExtensions` | `EricksonLopez.Concurrency.AspNetCore.DependencyInjection` | Extension `AddConcurrencyAspNetCore()` for registering web pipeline services. | `IServiceCollection` | ASP.NET Core web application service registration. | Basic | Yes (Level 02) |
| `ConcurrencyConflictMiddleware` | `EricksonLopez.Concurrency.AspNetCore.Middleware` | Pipeline middleware intercepting `ConcurrencyException` and emitting RFC 7807 / RFC 9457 HTTP 409 Conflict. | `RequestDelegate` | Global handling of concurrency collisions in HTTP pipelines. | Intermediate | Yes (Level 11) |
| `ConcurrencyMiddlewareExtensions` | `EricksonLopez.Concurrency.AspNetCore.Extensions` | Extension `UseConcurrencyConflictHandling()` enabling the middleware on `IApplicationBuilder`. | `IApplicationBuilder` | Configuring the HTTP middleware pipeline in `Program.cs`. | Basic | Yes (Level 11) |
| `ConcurrencyProblemDetails` | `EricksonLopez.Concurrency.AspNetCore.Models` | Strongly typed model extending `ProblemDetails` with entity metadata, versions, and conflict classifications. | `ProblemDetails` | Standardized RESTful error responses for concurrency conflicts. | Basic | Yes (Level 10) |
| `ConcurrencyConflictHttpResult` | `EricksonLopez.Concurrency.AspNetCore.Extensions` | Custom `IResult` implementation for ASP.NET Core Minimal API endpoints. | `IResult`, `ConcurrencyProblemDetails` | Direct, zero-exception conflict responses in Minimal APIs. | Intermediate | Yes (Level 11) |
| `ConcurrencyResultExtensions` | `EricksonLopez.Concurrency.AspNetCore.Extensions` | Extension methods for `IResultExtensions` (`Results.Extensions.ConcurrencyConflict`). | `IResultExtensions` | Fluent return of conflict HTTP results in Minimal APIs. | Basic | Yes (Level 11) |
| `ConcurrencyHttpExtensions` | `EricksonLopez.Concurrency.AspNetCore.Extensions` | Helpers for extracting and injecting `ETag`, `If-Match`, and `If-None-Match` in HTTP requests/responses. | `HttpRequest`, `HttpResponse` | Bi-directional mapping between HTTP headers and `ConcurrencyToken`. | Basic | Yes (Level 11) |

---

## 4. `EricksonLopez.Concurrency.Dapper` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `ConcurrencyDapperExtensions` | `EricksonLopez.Concurrency.Dapper` | Extension methods on `IDbConnection` for zero-roundtrip conditional updates (`ExecuteOptimisticAsync`). | `IDbConnection`, `Dapper` | Atomic relational database writes without preceding `SELECT` queries. | Intermediate | Yes (Level 04) |
| `OptimisticUpdateBuilder` | `EricksonLopez.Concurrency.Dapper` | Builder for generating parameterized SQL: `UPDATE ... WHERE id = @Id AND version = @ExpectedVersion`. | None | Dynamic conditional SQL generation with multi-tenant partitioning support. | Intermediate | Yes (Levels 04, 10) |
| `DapperConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.Dapper.DependencyInjection` | Method `AddEricksonLopezConcurrencyDapper()` registering Dapper type handlers. | `IServiceCollection` | Transparent mapping of `ConcurrencyVersion` and `ConcurrencyToken` in Dapper. | Basic | Yes (Level 02) |

---

## 5. `EricksonLopez.Concurrency.MariaDb` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `MariaDbConcurrencyErrorClassifier` | `EricksonLopez.Concurrency.MariaDb` | Classifies MariaDB errors (deadlock 1213, timeout 1205, duplicate key 1062) into `ConcurrencyConflict`. | `Exception` | Intercepting MariaDB persistence exceptions and mapping to operational classifications. | Intermediate | Yes (Level 06) |
| `MariaDbLockExtensions` | `EricksonLopez.Concurrency.MariaDb` | Methods `WithMariaDbLock()` and `WithMariaDbLockWait(seconds)` for generating lock clauses. | `string` | Controlled pessimistic row locking with wait timeouts in MariaDB. | Intermediate | Yes (Levels 09, 11) |
| `MariaDbLockMode` | `EricksonLopez.Concurrency.MariaDb` | Enum defining MariaDB locking modes (`ForUpdate`, `ForUpdateNoWait`, `ForUpdateWait`, `LockInShareMode`). | None | Strongly typed lock clause selection in MariaDB queries. | Basic | Yes (Levels 09, 11) |
| `MariaDbConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.MariaDb.DependencyInjection` | Extension `AddEricksonLopezConcurrencyMariaDb()` registering the classifier in DI. | `IServiceCollection` | Configuring MariaDB support in DI container. | Basic | Yes (Level 02) |

---

## 6. `EricksonLopez.Concurrency.Mediator` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `IConcurrencyAwareRequest` | `EricksonLopez.Concurrency.Mediator` | Marker contract for CQRS commands carrying concurrency preconditions (`ExpectedVersion` or `ExpectedToken`). | None | Generic CQRS commands requiring version tracking. | Basic | Yes (Level 10) |
| `IConcurrencyAwareRequest<TResponse>` | `EricksonLopez.Concurrency.Mediator` | Strongly typed contract associating a concurrency command with its response type. | `ICommand<TResponse>` | CQRS commands dispatched via `EricksonLopez.Mediator`. | Intermediate | Yes (Level 10) |
| `ConcurrencyBehavior<TRequest, TResponse>` | `EricksonLopez.Concurrency.Mediator` | Observability pipeline behavior tracking telemetry spans, metrics, and outcomes on commands. | `IPipelineBehavior<TRequest, TResponse>` | Automatic OpenTelemetry instrumentation for concurrent CQRS commands. | Advanced | Yes (Level 10) |
| `ConcurrencyMediatorServiceCollectionExtensions` | `EricksonLopez.Concurrency.Mediator.DependencyInjection` | Extension `AddConcurrencyMediatorBehavior()` registering the behavior in DI. | `IServiceCollection` | Registering CQRS concurrency behavior in `Program.cs`. | Basic | Yes (Level 02) |

---

## 7. `EricksonLopez.Concurrency.MySql` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `MySqlConcurrencyErrorClassifier` | `EricksonLopez.Concurrency.MySql` | Classifies native MySQL errors (1213 Deadlock, 1205 Lock Timeout, 1062 Duplicate Key). | `Exception` | Translating MySqlConnector numeric error codes into structured `ConcurrencyConflict`. | Intermediate | Yes (Level 06) |
| `MySqlLockExtensions` | `EricksonLopez.Concurrency.MySql` | Extension `WithMySqlLock(MySqlLockMode)` injecting locking clauses into SQL statements. | `string` | Safe pessimistic queries with `FOR UPDATE NOWAIT` or `SKIP LOCKED`. | Intermediate | Yes (Level 09) |
| `MySqlLockMode` | `EricksonLopez.Concurrency.MySql` | Enum defining MySQL lock modes (`ForUpdate`, `ForUpdateNoWait`, `ForUpdateSkipLocked`, `ForShare`). | None | Selecting pessimistic lock syntax in MySQL 8+. | Basic | Yes (Level 09) |
| `MySqlConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.MySql.DependencyInjection` | Extension `AddEricksonLopezConcurrencyMySql()` registering the classifier in DI. | `IServiceCollection` | Configuring MySQL in the composition root. | Basic | Yes (Level 02) |

---

## 8. `EricksonLopez.Concurrency.Oracle` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `OracleConcurrencyErrorClassifier` | `EricksonLopez.Concurrency.Oracle` | Classifies Oracle errors (ORA-00060 Deadlock, ORA-00054 Resource Busy, ORA-08177 Serialization). | `Exception` | Intercepting Oracle ORA exceptions and mapping to transient conflicts. | Intermediate | Yes (Levels 06, 11) |
| `OracleLockExtensions` | `EricksonLopez.Concurrency.Oracle` | Extensions `WithOracleLock()` and `WithOracleLockWait(seconds)` generating Oracle lock syntax. | `string` | Injecting `FOR UPDATE WAIT n` or `FOR UPDATE NOWAIT` into Oracle SQL. | Intermediate | Yes (Levels 09, 11) |
| `OracleLockMode` | `EricksonLopez.Concurrency.Oracle` | Enum defining Oracle locking modes (`ForUpdate`, `ForUpdateNoWait`, `ForUpdateWait`, `ForUpdateSkipLocked`). | None | Strongly typed pessimistic locking options for Oracle Database. | Basic | Yes (Levels 09, 11) |
| `OracleRowScnToken` | `EricksonLopez.Concurrency.Oracle` | Record struct modeling the `ORA_ROWSCN` 64-bit System Change Number pseudo-column. | `IConcurrencyToken` | Native optimistic concurrency in Oracle without adding custom version columns. | Intermediate | Yes (Level 09) |
| `OracleConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.Oracle.DependencyInjection` | Extension `AddEricksonLopezConcurrencyOracle()` registering the classifier in DI. | `IServiceCollection` | Registering Oracle support in DI. | Basic | Yes (Level 02) |

---

## 9. `EricksonLopez.Concurrency.PostgreSql` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `PostgreSqlConcurrencyErrorClassifier` | `EricksonLopez.Concurrency.PostgreSql` | Classifies PostgreSQL SQLSTATE codes (`40001` serialization, `40P01` deadlock, `55P03` lock unavailable). | `PostgresException` | Detecting transient collisions in PostgreSQL transactions. | Intermediate | Yes (Level 06) |
| `PostgreSqlLockExtensions` | `EricksonLopez.Concurrency.PostgreSql` | Extension `WithLock(PostgreSqlLockMode)` appending `FOR UPDATE` clauses to SQL. | `string` | Row-level locking and pessimistic concurrency in PostgreSQL. | Intermediate | Yes (Levels 09, 11) |
| `PostgreSqlLockMode` | `EricksonLopez.Concurrency.PostgreSql` | Enum defining lock modes (`ForUpdate`, `ForNoKeyUpdate`, `ForShare`, `ForKeyShare`, `SkipLocked`, etc.). | None | Fine-grained row locking level configuration in PostgreSQL. | Basic | Yes (Levels 09, 11) |
| `XminConcurrencyToken` | `EricksonLopez.Concurrency.PostgreSql` | Record struct modeling the hidden system `xmin` 32-bit transaction ID column. | `IConcurrencyToken` | Detecting row modifications in PostgreSQL without schema alterations. | Intermediate | Yes (Level 09) |
| `PostgreSqlConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.PostgreSql.DependencyInjection` | Extension `AddEricksonLopezConcurrencyPostgreSql()` registering the classifier in DI. | `IServiceCollection` | Dependency injection for Npgsql-based projects. | Basic | Yes (Level 02) |

---

## 10. `EricksonLopez.Concurrency.Result` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `ConcurrencyErrors` | `EricksonLopez.Concurrency.Result` | Static factory creating strongly typed `Error.Conflict` descriptors from `ConcurrencyConflict`. | `ConcurrencyConflict`, `Error` | Direct, decoupled translation to the `Result<T>` monad. | Basic | Yes (Level 06) |
| `ConcurrencyResultExtensions` | `EricksonLopez.Concurrency.Result` | Functional extension methods `ToResult()` and `FromRowsAffected()` translating outcomes to `Result<T>`. | `Result`, `CasResult<T>`, `ConflictResolution<T>` | Exception-free monadic returns in repositories and command handlers. | Intermediate | Yes (Levels 04, 08) |

---

## 11. `EricksonLopez.Concurrency.Sqlite` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `SqliteConcurrencyErrorClassifier` | `EricksonLopez.Concurrency.Sqlite` | Classifies SQLite error codes (`SQLITE_BUSY` 5, `SQLITE_LOCKED` 6, `SQLITE_CONSTRAINT` 19). | `SqliteException` | Detecting file-level contention in embedded SQLite databases. | Intermediate | Yes (Level 06) |
| `SqliteConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.Sqlite.DependencyInjection` | Extension `AddEricksonLopezConcurrencySqlite()` registering the classifier in DI. | `IServiceCollection` | Registering SQLite support in DI. | Basic | Yes (Level 02) |

---

## 12. `EricksonLopez.Concurrency.SqlServer` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `SqlServerErrorClassifier` | `EricksonLopez.Concurrency.SqlServer` | Classifies SQL Server error codes (1205 Deadlock, 3960 Snapshot Conflict, 1222 Lock Timeout). | `SqlException` | Intercepting collisions and deadlocks in Microsoft SQL Server / Azure SQL. | Intermediate | Yes (Levels 06, 11) |
| `SqlServerLockExtensions` | `EricksonLopez.Concurrency.SqlServer` | Extension `WithSqlServerTableHint(SqlServerLockMode)` injecting table hints such as `UPDLOCK, ROWLOCK`. | `string` | Preventing deadlocks in SQL Server via optimized T-SQL table hints. | Intermediate | Yes (Level 09) |
| `SqlServerLockMode` | `EricksonLopez.Concurrency.SqlServer` | Enum with hint combinations (`UpdLockRowLock`, `UpdLockRowLockNowait`, `XLockRowLock`, etc.). | None | Selecting concurrency hints in T-SQL queries. | Basic | Yes (Level 09) |
| `SqlServerRowVersionToken` | `EricksonLopez.Concurrency.SqlServer` | Record struct encapsulating 8-byte binary `ROWVERSION` / `TIMESTAMP` columns with in-memory comparison. | `IConcurrencyToken` | Transparent synchronization with SQL Server automatic `ROWVERSION` columns. | Intermediate | Yes (Levels 09, 11) |
| `SqlServerConcurrencyServiceCollectionExtensions` | `EricksonLopez.Concurrency.SqlServer.DependencyInjection` | Extension `AddEricksonLopezConcurrencySqlServer()` registering the classifier in DI. | `IServiceCollection` | Configuring SQL Server support in DI. | Basic | Yes (Level 02) |

---

## 13. `EricksonLopez.Concurrency.Testing` (Infrastructure)

| Type | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Showcase Example |
|---|---|---|---|---|:---:|:---:|
| `FakeConcurrencyController` | `EricksonLopez.Concurrency.Testing` | High-fidelity test double for `IConcurrencyController` with programmable outcomes and invocation history. | `IConcurrencyController` | Unit testing use cases and command handlers without reflection-based mocks. | Intermediate | Yes (Levels 10, 11) |
| `ConcurrencyConflictBuilder` | `EricksonLopez.Concurrency.Testing` | Fluent test data builder for programmatically assembling `ConcurrencyConflict` instances. | `ConcurrencyConflict` | Assembling collision scenarios in unit and integration tests. | Basic | Yes (Levels 10, 11) |
| `VerifyVersionInvocation` | `EricksonLopez.Concurrency.Testing` | Immutable record capturing invocation arguments from `VerifyVersion`. | `ExpectedVersion` | Asserting version check calls in tests with `FakeConcurrencyController`. | Basic | Yes (Level 10) |
| `VerifyTokenInvocation` | `EricksonLopez.Concurrency.Testing` | Immutable record capturing invocation arguments from `VerifyToken`. | `IConcurrencyToken` | Asserting token verification calls in tests. | Basic | Yes (Level 11) |
| `ExecuteCasInvocation` | `EricksonLopez.Concurrency.Testing` | Immutable record capturing invocation arguments from `ExecuteCasAsync`. | `ExpectedVersion` | Asserting Compare-And-Swap invocations in unit tests. | Basic | Yes (Level 11) |

---

## 🎯 Coverage & Integrity Summary

- **Total Public Elements Discovered**: 80
- **Total Public Elements Covered in Showcase**: 80 (100% Coverage)
- **Fictitious or Invented APIs**: 0 (0% Fictitious Code)
- **Obsolete or Non-Existent APIs**: 0

### Recent Synchronization Changes (v-current)

| Change | Location | Type |
|---|---|---|
| CS8602 null-dereference fix | `Level11_ComprehensiveApiCoverageDemo.cs:209` | Bug Fix |
| `IConcurrencyAware.ConcurrencyToken?` nullability comment | `Level11_ComprehensiveApiCoverageDemo.cs` | Documentation |
| `default(ExpectedVersion) == Specific(0)` clarification | `Level03_RealWorldUseCases.cs` | Documentation |
| `ExecuteCasAsync` non-reentrancy architectural note | `Level05_ProcessingAndConcurrency.cs` | Documentation |
| ADR-001 Concurrency vs Resilience section | `Level00_Conceptual.cs` | Documentation |
| `StripeCount` behavioral comment (internal lock partitioning) | `Level02_FullConfiguration.cs` | Documentation |
| `ConflictResolution.Merged()` direct factory mention | `Level08_CustomizationAndExtensibility.cs` | Coverage |
| **[16] Metadata propagation pipeline** (`WithMetadata()` → `Error.Metadata` → `ProblemDetails.Extensions`) | `Level11_ComprehensiveApiCoverageDemo.cs` | New Example |
| Structured `<remarks>` cookbook headers on all 12 levels | L00–L11 | Documentation |
| `README.md` entry point created | `samples/EricksonLopez.Concurrency.Showcase/README.md` | Documentation |
