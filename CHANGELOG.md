# Changelog

All notable changes to `EricksonLopez.Concurrency` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
 
## [2.0.0] - 2026-09-23

### Breaking Changes
- **BC-001 (`EricksonLopez.Concurrency`): Mandatory Version Progression or `IMutableVersionedEntity` Implementation in `ExecuteCasAsync`**:
  - *Previous Behavior*: Entities implementing `IVersionedEntity` without mutating their `Version` in the `mutate` delegate succeeded; the controller computed and returned the incremented version in `CasResult<TEntity>`.
  - *Current Behavior*: If an entity does not implement `IMutableVersionedEntity` and its mutation delegate does not produce an instance whose `Version` matches `nextVersion.Value`, `ConcurrencyController.ExecuteCasAsync` throws `InvalidOperationException`.
  - *Affected Consumers*: Consumers mutating domain entities or immutable records via `ExecuteCasAsync` where the delegate did not manually advance `Version`.
  - *Migration Guidance*: For mutable entities, implement the new `IMutableVersionedEntity` interface. For immutable records, ensure the `mutate` delegate returns a new instance with `Version` incremented (e.g., `entity with { Version = entity.Version + 1 }`).

- **BC-002 (`EricksonLopez.Concurrency`): Fail-Fast Reentrancy Guards on `ConcurrencyController.ExecuteCasAsync`**:
  - *Previous Behavior*: Reentrant or nested calls to `ExecuteCasAsync` for the same `entityId` within the same asynchronous flow executed without restriction.
  - *Current Behavior*: Calls are tracked via `AsyncLocal<ReentrancyNode>`. Recursive or nested calls on the same `entityId` throw `InvalidOperationException` to eliminate deadlocks.
  - *Affected Consumers*: Workflows invoking nested CAS operations on the same entity within domain event handlers or nested delegates.
  - *Migration Guidance*: Decouple nested domain operations so that CAS operations on a given aggregate ID are executed sequentially or orchestrated at the outer application layer.

- **BC-003 (`EricksonLopez.Concurrency`): Bounded Timeouts for Lock Acquisition and Mutation in `ExecuteCasAsync`**:
  - *Previous Behavior*: `ExecuteCasAsync` had no internal timeout guards and waited indefinitely.
  - *Current Behavior*: Enforces `ConcurrencyOptions.DefaultMaxAcquisitionTimeout` (10s) and `DefaultMaxExecutionTimeout` (30s), throwing `TimeoutException` if either threshold is exceeded.
  - *Affected Consumers*: Systems experiencing severe contention or workloads executing long-running mutation delegates.
  - *Migration Guidance*: Adjust `DefaultMaxAcquisitionTimeout` and `DefaultMaxExecutionTimeout` via `AddEricksonLopezConcurrency(options => ...)` or move long-running external I/O out of the CAS mutation delegate.

- **BC-004 (`EricksonLopez.Concurrency`): Strict Identifier Validation on `VerifyVersion`, `VerifyToken`, and `ExecuteCasAsync`**:
  - *Previous Behavior*: Passing `null`, empty string, or whitespace as `entityId` was passed through without validation.
  - *Current Behavior*: Throws `ArgumentException` (`ArgumentNullException`) if `entityId` is `null`, empty, or whitespace.
  - *Affected Consumers*: Code paths passing default, uninitialized, or empty entity identifier strings.
  - *Migration Guidance*: Ensure all entity identifiers passed to `IConcurrencyController` are non-empty, non-whitespace strings.

- **BC-005 (`EricksonLopez.Concurrency.Abstractions`): Nullable Reference Type Change on `IConcurrencyAware.ConcurrencyToken`**:
  - *Previous Behavior*: Declared as non-nullable `IConcurrencyToken ConcurrencyToken { get; }`.
  - *Current Behavior*: Declared as nullable `IConcurrencyToken? ConcurrencyToken { get; }`.
  - *Affected Consumers*: Consumers with `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` dereferencing `entity.ConcurrencyToken` without null checking.
  - *Migration Guidance*: Use null-safe navigation (e.g., `entity.ConcurrencyToken?.Value`) or provide a fallback (`entity.ConcurrencyToken ?? ConcurrencyToken.None`).

- **BC-006 (`EricksonLopez.Concurrency.Dapper`): Strict Single-Row Requirement in `ExecuteOptimisticAsync` and `ExecuteOptimisticTokenAsync`**:
  - *Previous Behavior*: If `rowsAffected > 1`, the methods recorded a success and returned `null`.
  - *Current Behavior*: Throws `InvalidOperationException` if `rowsAffected > 1` (`"Optimistic update affected N rows... Expected exactly 1 row."`).
  - *Affected Consumers*: SQL queries or stored procedures updating batches or multiple records.
  - *Migration Guidance*: Ensure optimistic update queries contain specific unique key predicates targeting exactly one record.

- **BC-007 (`EricksonLopez.Concurrency.Dapper`): Strict Identifier Character Validation in `OptimisticUpdateBuilder.BuildVersionedUpdate`**:
  - *Previous Behavior*: Accepted escaped SQL identifiers containing brackets `[table]` or quotes `"table"`.
  - *Current Behavior*: Identifiers are validated against `char.IsAsciiLetterOrDigit`, `_`, and `.`. Characters like `[` or `]` throw `ArgumentException`.
  - *Affected Consumers*: Queries using bracketed or quoted table/column names in `BuildVersionedUpdate`.
  - *Migration Guidance*: Pass plain, unquoted identifiers (e.g., `Orders` instead of `[Orders]`).

- **BC-008 (Database Dialects): Reclassification of Concurrency Conflict Types across Database Providers**:
  - *Previous Behavior*: Dialect error classifiers mapped certain conditions to generic `SerializationFailure` or `Custom`.
  - *Current Behavior*:
    - Deadlocks in MariaDB (1213), MySQL (1213), and Oracle (ORA-00060) now return `ConcurrencyConflictType.Deadlock`.
    - Lock timeouts in MariaDB (1205), MySQL (1205), Oracle (ORA-00054), and SQLite (5, 6) now return `ConcurrencyConflictType.LockUnavailable`.
    - Unique constraint violations in MariaDB (1062), MySQL (1062), Oracle (ORA-00001), SQL Server (2601/2627), and SQLite (19) now return `ConcurrencyConflictType.AlreadyExists`.
    - SQL Server lock timeouts (1222) now return `ConcurrencyConflictType.LockUnavailable`.
  - *Affected Consumers*: Exception handlers or retry policies matching on `conflict.ConflictType`.
  - *Migration Guidance*: Update pattern matching and policy rules to handle `Deadlock`, `LockUnavailable`, and `AlreadyExists`.

- **BC-009 (`EricksonLopez.Concurrency.Result` & `EricksonLopez.Concurrency.PostgreSql`): Removal of Transitive ProjectReference to `EricksonLopez.Concurrency`**:
  - *Previous Behavior*: Installing `EricksonLopez.Concurrency.Result` or `EricksonLopez.Concurrency.PostgreSql` transitively pulled in `EricksonLopez.Concurrency` (Core).
  - *Current Behavior*: Project references to core were removed to isolate engine dependencies; they now only reference `EricksonLopez.Concurrency.Abstractions`.
  - *Affected Consumers*: Projects relying on core types (`ConcurrencyController`, `ConcurrencyOptions`) without an explicit package reference.
  - *Migration Guidance*: Add an explicit reference to `<PackageReference Include="EricksonLopez.Concurrency" Version="2.0.0" />`.

- **BC-010 (`EricksonLopez.Concurrency.Oracle`): Semicolon Removal in `OracleLockExtensions.WithLock` and `WithOracleLockWait`**:
  - *Previous Behavior*: Appended a trailing semicolon `;` to the generated SQL query.
  - *Current Behavior*: Emits SQL without a trailing semicolon (preventing ORA-00911 in ODP.NET).
  - *Affected Consumers*: Consumers or tests expecting queries to end with a semicolon.
  - *Migration Guidance*: Update query formatting expectations or tests to expect query strings without trailing semicolons.

- **BC-011 (`EricksonLopez.Concurrency.SqlServer`): Direct Byte Span Equality in `SqlServerRowVersionToken.Equals`**:
  - *Previous Behavior*: Tokens were compared by string hexadecimal equality. `default(SqlServerRowVersionToken)` equaled `new SqlServerRowVersionToken(Array.Empty<byte>())`.
  - *Current Behavior*: Compares raw byte spans. `default` (`_bytes == null`) and empty byte array (`_bytes == []`) are no longer considered equal.
  - *Affected Consumers*: Code comparing uninitialized tokens with empty byte tokens.
  - *Migration Guidance*: Initialize rowversion tokens consistently using valid byte spans.

- **BC-012 (`EricksonLopez.Concurrency.Abstractions`): Non-Null Default Value Semantics for `ConcurrencyToken` Properties**:
  - *Previous Behavior*: Auto-properties in `default(ConcurrencyToken)` returned `Value == null` and `TokenKind == null`.
  - *Current Behavior*: Fallbacks return `Value == string.Empty` and `TokenKind == "None"`.
  - *Affected Consumers*: Code checking for `null` instead of empty or `"None"`.
  - *Migration Guidance*: Update null checks to `string.IsNullOrEmpty(token.Value)` and `token.TokenKind == "None"`.

### Added
- **`EricksonLopez.Concurrency.Abstractions`**:
  - `IMutableVersionedEntity`: Contract allowing optimistic concurrency controllers to mutate version state in place on mutable entities.
  - `IConcurrencyController.ExecuteCasAsync`: Synchronous mutation delegate overload (`Func<TEntity, CancellationToken, TEntity>`) with default interface implementation.
- **`EricksonLopez.Concurrency` (Core)**:
  - `ConcurrencyOptions.DefaultMaxAcquisitionTimeout`: Configurable lock acquisition timeout (default 10s) preventing deadlocks during in-memory CAS operations.
  - `ConcurrencyOptions.DefaultMaxExecutionTimeout`: Configurable execution timeout (default 30s) protecting against long-running or stalled mutation delegates.
  - `IDisposable` and `IAsyncDisposable` support on `ConcurrencyController` for deterministic cleanup of pooled resources and lock synchronization primitives.
- **Samples & Quality Automation**:
  - `Showcase Level 11` (`Level11_ComprehensiveApiCoverageDemo.cs`): Executable verification covering complete API contracts, in-memory reentrancy detection, and database dialect error classifications.
  - `scripts/verify-benchmark-gate.ps1` & test suite: Automated PR benchmark regression verification asserting zero allocations on hot paths and latency within thresholds.

### Changed
- **`ConcurrencyController`**:
  - In-memory Compare-And-Swap (`ExecuteCasAsync`) now enforces per-entity mutual exclusion using striped, object-pooled `RefCountedLock` semaphores.
  - Added non-reentrant execution guards via `AsyncLocal<ReentrancyNode>` throwing `InvalidOperationException` upon recursive calls for the same `entityId`.
  - Automatically advances `IMutableVersionedEntity.Version` upon successful mutation, or validates that immutable record delegates produced an incremented version.
- **Mutation Testing**:
  - Transitioned from monolithic `stryker-config.json` to 13 dedicated per-package profiles (`stryker-*-config.json`), maintaining strict 100/98/95 thresholds and concurrency limits.

### Fixed
- **Static Analysis & Hygiene**:
  - Resolved `CA1816` analyzer warnings in `ConcurrencyBenchmarks` and `OptimisticContentionBenchmarks` by sealing types and invoking `GC.SuppressFinalize(this)`.

### Security
- Added deterministic lock acquisition and mutation execution timeout boundaries throwing `TimeoutException`, eliminating threadpool saturation risks under severe in-memory contention.

---

## [1.0.0] - 2026-08-29

### Added
- **`EricksonLopez.Concurrency.Abstractions`**:
  - Zero-allocation `ConcurrencyVersion` and typed `ConcurrencyVersion<TEntity>` `readonly record struct` value types with `ISpanFormattable`, `ISpanParsable`, and monotonic `Next()` transitions.
  - `ExpectedVersion` struct supporting `Specific(version)`, `Any`, `New`, and `Exists` pre-conditions.
  - `ActualVersion` struct with `NotFound` state for optimistic conflict evaluation.
  - `ConcurrencyToken` struct supporting opaque tokens (GUID, Byte array, String, ETag) with `TokenKind` discriminators.
  - Domain contracts: `IVersionedEntity`, `IVersionedEntity<TEntity>`, `IConcurrencyAware`, `IConcurrencyChecker`, `IConcurrencyController`, `IConcurrencyConflictResolver<TEntity>`.
  - Conflict modeling: `ConcurrencyConflict` sealed record with `ConcurrencyConflictType` and `ConcurrencyConflictClassification` (`Transient`, `Retryable`, `NonRetryable`, `StaleState`, `Fatal`).
  - Conflict resolution model: `ConflictResolution<TEntity>` and `ConflictResolutionStrategy` (`Reject`, `LastWriteWinsExplicit`, `MergeDomainSpecific`, `RefreshAndRetry`).
  - Exceptions: `ConcurrencyException`, `ConcurrencyConfigurationException`, `ConcurrencyTokenMismatchException`.
- **`EricksonLopez.Concurrency` (Core)**:
  - `ConcurrencyController` implementing in-memory atomic Compare-And-Swap (`ExecuteCasAsync`), version validation (`VerifyVersion`), and token verification (`VerifyToken`).
  - `OptimisticConcurrencyChecker` zero-allocation singleton evaluator.
  - `ConcurrencyOptions` configuration model and `AddEricksonLopezConcurrency()` dependency injection extensions.
  - Built-in conflict resolvers: `RejectConflictResolver<TEntity>`, `LastWriteWinsConflictResolver<TEntity>`, `DelegateConflictResolver<TEntity>`, `RefreshAndRetryConflictResolver<TEntity>`.
  - Distributed tracing and metrics with OpenTelemetry: `ConcurrencyDiagnostics` (`concurrency.conflicts`, `concurrency.successes`, `concurrency.failures`, `concurrency.merges`, `concurrency.duration`).
- **`EricksonLopez.Concurrency.AspNetCore`**:
  - `ConcurrencyConflictMiddleware`: Middleware pipeline component intercepting unhandled `ConcurrencyException` instances and returning RFC 7807 compliant HTTP 409 Conflict responses with matching `ETag` headers.
  - `ConcurrencyProblemDetails`: Strongly typed RFC 7807 `ProblemDetails` model capturing conflict type, classification, entity metadata, and versions.
  - `ConcurrencyResultExtensions`: Minimal API `IResult` extension method `Results.Extensions.ConcurrencyConflict(conflict)` for functional endpoints without exception throwing.
  - `ConcurrencyHttpExtensions`: HTTP request/response extensions for parsing and setting `If-Match`, `If-None-Match`, and `ETag` headers (`GetExpectedConcurrencyToken`, `GetExpectedConcurrencyVersion`, `SetConcurrencyETag`).
  - `ConcurrencyAspNetCoreServiceCollectionExtensions`: Dependency injection extension `services.AddConcurrencyAspNetCore()`.
- **`EricksonLopez.Concurrency.Testing`**:
  - `FakeConcurrencyController`: High-fidelity, mock-free in-memory test double for `IConcurrencyController` with complete invocation tracking (`VerifyVersionInvocations`, `VerifyTokenInvocations`, `ExecuteCasInvocations`), configurable canned outcomes (`WithSuccess`, `WithConflict`, `WithConflictOnNextWrite`), and programmable custom verification delegates (`WhenVerifyVersion`, `WhenVerifyToken`, `WhenExecuteCas`).
  - `ConcurrencyConflictBuilder`: Fluent test data builder for programmatic construction of `ConcurrencyConflict` instances with custom classifications, timestamps, and metadata.
- **`EricksonLopez.Concurrency.Result`**:
  - Extension methods `ToResult()` translating `CasResult<T>`, `ConcurrencyConflict?`, and `ConflictResolution<T>` into `Result` and `Result<T>` from `EricksonLopez.Result`.
  - `ConcurrencyResultExtensions.FromRowsAffected()` converting database write results into monadic results.
  - `ConcurrencyErrors` factory mapping conflicts into structured `Error` instances with error codes and metadata.
- **`EricksonLopez.Concurrency.Dapper`**:
  - `ConcurrencyDapperExtensions`: `ExecuteOptimisticAsync` and `ExecuteOptimisticTokenAsync` zero-roundtrip conditional SQL execution.
  - `OptimisticUpdateBuilder`: Dynamic SQL generation for optimistic updates with multi-tenancy support (`tenant_id`).
  - `DapperConcurrencyServiceCollectionExtensions`: DI registration extension `services.AddEricksonLopezConcurrencyDapper()`.
- **`EricksonLopez.Concurrency.Mediator`**:
  - `IConcurrencyAwareRequest` and `IConcurrencyAwareRequest<TResponse>` CQRS command contracts.
  - `ConcurrencyBehavior<TRequest, TResponse>` observability pipeline behavior (`sealed class`) for `EricksonLopez.Mediator` utilizing zero-allocation `INext<TResponse>` struct delegates.
  - `ConcurrencyMediatorServiceCollectionExtensions`: DI registration extension `services.AddConcurrencyMediatorBehavior()`.
- **Database Dialect Packages**:
  - `EricksonLopez.Concurrency.PostgreSql`: `XminConcurrencyToken` (`xmin` 32-bit transaction ID), `PostgreSqlConcurrencyErrorClassifier` (SQLSTATE `40001`, `40P01`, `55P03`, `23505`), `PostgreSqlLockExtensions.WithLock` (`FOR UPDATE`, `NOWAIT`, `SKIP LOCKED`, `FOR SHARE`).
  - `EricksonLopez.Concurrency.SqlServer`: `SqlServerRowVersionToken` (8-byte binary `ROWVERSION` / `TIMESTAMP`), `SqlServerErrorClassifier` (Errors `1205`, `3960`, `3961`, `1222`, `2601`, `2627`), `SqlServerLockExtensions.WithSqlServerTableHint` (`UPDLOCK`, `ROWLOCK`, `NOWAIT`, `READPAST`).
  - `EricksonLopez.Concurrency.MySql`: `MySqlConcurrencyErrorClassifier` (Errors `1213`, `1205`, `1062`), `MySqlLockExtensions.WithMySqlLock`.
  - `EricksonLopez.Concurrency.MariaDb`: `MariaDbConcurrencyErrorClassifier`, `MariaDbLockExtensions.WithMariaDbLock`, `WithMariaDbLockWait(seconds)`.
  - `EricksonLopez.Concurrency.Oracle`: `OracleRowScnToken` (64-bit `ORA_ROWSCN`), `OracleConcurrencyErrorClassifier` (`ORA-00060`, `ORA-00054`, `ORA-08177`, `ORA-00001`), `OracleLockExtensions.WithOracleLock`, `WithOracleLockWait(seconds)`.
  - `EricksonLopez.Concurrency.Sqlite`: `SqliteConcurrencyErrorClassifier` (`SQLITE_BUSY` 5, `SQLITE_LOCKED` 6, `SQLITE_CONSTRAINT` 19).
- **Multi-Targeting, Strong Naming & Quality Architecture**:
  - Multi-targeting support for `.NET 8.0` (`net8.0`), `.NET 9.0` (`net9.0`), and `.NET 10.0` (`net10.0`).
  - Strict Native AOT compatibility (`IsAotCompatible = true`, `EnableTrimAnalyzer = true`).
  - Strong naming assembly signing across all assemblies (`EricksonLopez.snk`).
- **Architectural Decision Records (ADRs)**:
  - ADR-001 through ADR-012 documenting the foundational architecture, zero-allocation design, database tokens, testing fakes, ASP.NET Core RFC 7807 integration, and conflict resolution lifecycle.
- **Reference & Learning Suite**:
  - `EricksonLopez.Concurrency.Showcase`: Progressive 11-level executable showcase covering conceptual foundations through enterprise architecture.
  - Complete test harness across 16 test projects including unit tests, integration race condition suites, architecture validation (`NetArchTest.Rules`), and Native AOT smoke execution.
