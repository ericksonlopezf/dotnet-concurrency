# Changelog

All notable changes to `EricksonLopez.Concurrency` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1](https://github.com/ericksonlopezf/dotnet-concurrency/compare/v1.0.0...v1.0.1) (2026-08-29)


### 🐛 Bug Fixes

* **quality:** resolve all 8 SonarCloud security and reliability issues ([0c97bbb](https://github.com/ericksonlopezf/dotnet-concurrency/commit/0c97bbbce791058b69264972639e6b73617de58c))

## [Unreleased]

<!-- Changes verified in the current development branch that have not yet been released. -->

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
