# Public API Inventory

This document provides a comprehensive inventory of public types, interfaces, value structs, and extension methods exported by the `EricksonLopez.Concurrency` package ecosystem.

---

## 1. `EricksonLopez.Concurrency.Abstractions`

### Interfaces
- `IConcurrencyController`: Primary contract for executing in-memory Compare-And-Swap (CAS) state mutations and evaluating version/token preconditions.
- `IConcurrencyChecker`: Contract for validating version and token equality without throwing exceptions.
- `IConcurrencyConflictResolver<TEntity>`: Contract for asynchronous conflict resolution and reconciliation strategies.
- `IConcurrencyErrorClassifier`: Contract for translating driver-specific exceptions into typed `ConcurrencyConflict`.
- `IConcurrencyToken`: Common interface for opaque concurrency tokens (e.g. ETags, hashes, binary rowversions).
- `IVersionedEntity`: Interface denoting an entity possessing a numeric monotonic `long Version` property.
- `IVersionedEntity<TVersion>`: Interface denoting an entity with a generic strongly typed version property.
- `IConcurrencyAwareRequest`: Marker interface for CQRS commands carrying expected concurrency tokens or versions.

### Value Types (Readonly Record Structs)
- `ConcurrencyVersion`: Readonly record struct wrapping `long Value`. Implements `IComparable<ConcurrencyVersion>`, `IEquatable<ConcurrencyVersion>`, `ISpanParsable<ConcurrencyVersion>`, `IParsable<ConcurrencyVersion>`.
- `ConcurrencyVersion<TEntity>`: Strongly-typed readonly record struct tied to a domain entity type.
- `ConcurrencyToken`: Readonly record struct wrapping an opaque string value and an optional token kind/discriminator.
- `ExpectedVersion`: Readonly record struct expressing version preconditions (`Any`, `None`, `Specific(long)`).
- `ActualVersion`: Readonly record struct wrapping the persistent state version found in storage.
- `CasResult<TEntity>`: Readonly record struct encapsulating the result of a Compare-And-Swap operation (`IsSuccess`, `IsConflict`, `Entity`, `NewVersion`, `Conflict`).

### Domain & Diagnostic Models
- `ConcurrencyConflict`: Immutable record containing rich conflict metadata (`EntityId`, `EntityType`, `ExpectedVersion`, `ActualVersion`, `ExpectedToken`, `ActualToken`, `ConflictType`, `Classification`, `Operation`, `Message`, `Timestamp`, `Metadata`).
- `ConcurrencyConflictType`: Enum specifying the conflict nature (`VersionMismatch`, `TokenMismatch`, `StateDeleted`, `AlreadyExists`, `SerializationFailure`, `Deadlock`, `LockUnavailable`, `Custom`).
- `ConcurrencyConflictClassification`: Enum specifying operational classification (`Transient`, `StaleState`, `NonRetryable`, `Fatal`).
- `ConflictResolution<TEntity>`: Immutable record containing the resolved state or rejection reason.
- `ConflictResolutionStrategy`: Enum denoting the resolution action (`None`, `RefreshAndRetry`, `ClientResolution`, `Rejected`).
- `ConcurrencyException`: Specialized domain exception thrown when an unhandled concurrency conflict cannot be reconciled.

---

## 2. `EricksonLopez.Concurrency` (Core)

### Implementations & Controllers
- `ConcurrencyController`: Production implementation of `IConcurrencyController` with OpenTelemetry metrics and Activity instrumentation.
- `OptimisticConcurrencyChecker`: Singleton and thread-safe implementation of `IConcurrencyChecker`.
- `RefreshAndRetryConflictResolver<TEntity>`: Configurable conflict resolver supporting auto-reloading and optional domain state merging.

### Dependency Injection
- `ConcurrencyServiceCollectionExtensions`: Extension methods for registering core concurrency services (`AddConcurrencyCore()`, `AddConcurrencyResolver<TEntity>()`).

---

## 3. `EricksonLopez.Concurrency.AspNetCore`

### Middleware & Results
- `ConcurrencyConflictMiddleware`: ASP.NET Core middleware intercepting `ConcurrencyException` and formatting RFC 7807 `ConcurrencyProblemDetails`.
- `ConcurrencyProblemDetails`: Strongly typed RFC 7807 ProblemDetails payload.
- `ConcurrencyResultsExtensions`: Minimal API `IResult` factory methods (`Results.Extensions.ConcurrencyConflict(...)`).
- `ConcurrencyHttpContextExtensions`: Helpers for reading/writing `ETag`, `If-Match`, and `If-None-Match` headers.

---

## 4. `EricksonLopez.Concurrency.Dapper`

### Type Handlers
- `ConcurrencyVersionTypeHandler`: Dapper `SqlMapper.TypeHandler<ConcurrencyVersion>` for seamless `BIGINT` mapping.
- `ConcurrencyTokenTypeHandler`: Dapper `SqlMapper.TypeHandler<ConcurrencyToken>` for string/varchar column mapping.
- `DapperConcurrencyExtensions`: Helper methods for registering type handlers into Dapper `SqlMapper`.

---

## 5. `EricksonLopez.Concurrency.Result`

### Functional Extensions
- `ConcurrencyResultExtensions`: Conversion methods from `CasResult<TEntity>` to `Result<TEntity>` and `ConflictResolution<TEntity>` to `Result<TEntity>`.
- `ConcurrencyErrors`: Typed factory methods generating structured `Error.Conflict` descriptors.

---

## 6. `EricksonLopez.Concurrency.Mediator`

### Pipeline Behaviors
- `ConcurrencyBehavior<TRequest, TResponse>`: Pipeline behavior for `EricksonLopez.Mediator` that captures concurrency metrics and telemetry on requests implementing `IConcurrencyAwareRequest`.

---

## 7. `EricksonLopez.Concurrency.Testing`

### Test Doubles & Builders
- `FakeConcurrencyController`: In-memory implementation of `IConcurrencyController` with configurable conflict simulation and invocation history.
- `ConcurrencyConflictBuilder`: Fluent builder for constructing complex test `ConcurrencyConflict` instances.
