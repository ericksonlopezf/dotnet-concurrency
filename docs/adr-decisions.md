# Architecture Decision Records (ADRs)

This document formally records all architectural and product decisions governing the `EricksonLopez.Concurrency` ecosystem.

---

## ADR-001: Separation of Concurrency Detection from Resilience Retries

- **Status**: Accepted
- **Context**: Concurrency conflicts can occur due to stale user updates or transient database race conditions. Combining automatic retry logic into the concurrency layer couples it to retry policies, backoff strategies, and resilience mechanics.
- **Decision**: `EricksonLopez.Concurrency` is strictly responsible for detecting, classifying, and reporting conflicts (`Transient`, `StaleState`, `NonRetryable`). Retries must be orchestrated by `EricksonLopez.Resilience` or explicit application policies.
- **Consequences**: Zero circular dependencies between Concurrency and Resilience; clean Single Responsibility Principle.

---

## ADR-002: Rejection of Distributed Locks and Generic Mutexes in Core

- **Status**: Accepted
- **Context**: Distributed locks (e.g. Redis Redlock, Consul mutexes) introduce external network dependencies, deadlocks, lease expiration hazards, and high latency.
- **Decision**: Exclude distributed locks, Redis locks, and generic mutex wrappers from `EricksonLopez.Concurrency`. Concurrency control in this framework is optimistic-first and database-arbitrated.
- **Consequences**: Ultra-high throughput, predictable execution paths, no external cluster lock dependencies.

---

## ADR-003: Readonly Record Structs for Versions and Tokens

- **Status**: Accepted
- **Context**: Version numbers and tokens are evaluated on every write path across high-frequency transaction pipelines. Allocating heap objects for each check creates unnecessary GC pressure.
- **Decision**: Represent `ConcurrencyVersion`, `ConcurrencyVersion<T>`, `ConcurrencyToken`, `ExpectedVersion`, `ActualVersion`, and `XminConcurrencyToken` as `readonly record struct` value types.
- **Consequences**: Zero heap allocations on check paths, sub-nanosecond comparison performance.

---

## ADR-004: Native AOT First Architecture and Zero Runtime Reflection

- **Status**: Accepted
- **Context**: Modern microservices require rapid startup, low memory footprint, and Native AOT compilation compatibility.
- **Decision**: Enforce `EnableTrimAnalyzer=true` and `TreatWarningsAsErrors=true`. All dependency injection extensions and serializers must avoid unannotated runtime reflection.
- **Consequences**: 100% Native AOT trimming safe; fast cold starts in containerized environments.

---

## ADR-005: First-Class Integration with EricksonLopez.Result and EricksonLopez.Mediator

- **Status**: Accepted
- **Context**: The EricksonLopez ecosystem utilizes functional error handling via `Result<T>` and zero-allocation CQRS via `EricksonLopez.Mediator`.
- **Decision**: Provide dedicated integration packages `EricksonLopez.Concurrency.Result` and `EricksonLopez.Concurrency.Mediator` that translate conflicts into typed `Error` models and observability pipeline behaviors.
- **Consequences**: Harmonious developer experience across all tier-1 ecosystem libraries without polluting core abstractions.

### ADR-005 Addendum: ConcurrencyBehavior is Observability-Only

`ConcurrencyBehavior<TRequest, TResponse>` is intentionally scoped to **observability** (OpenTelemetry spans + conflict metrics), not version enforcement. It does not automatically verify `ExpectedVersion` from `IConcurrencyAwareRequest` against entities in storage or memory because:

1. The behavior has no knowledge of the specific entity type or repository used by the handler.
2. Performing reads inside a pipeline behavior would introduce implicit coupling between the behavior and the persistence layer.
3. Version enforcement belongs at the **handler boundary**, where `IConcurrencyController` has full context about the entity being mutated.

> **Note**: `ConcurrencyBehavior<TRequest, TResponse>` is implemented as a `sealed class`, not a `struct`. The zero-allocation property of the mediator pipeline is provided by the `INext<TResponse>` struct delegates from `EricksonLopez.Mediator`, not by the behavior class itself.

This distinction must be clearly communicated in documentation. See `docs/mediator-integration.md` for the correct usage pattern.

---

## ADR-006: Dedicated Database Dialect Packages for Engine Isolation

- **Status**: Accepted
- **Context**: Database-specific error classification (PostgreSQL SQLSTATE, SQL Server error numbers, MySQL/MariaDB codes, Oracle ORA codes, SQLite status codes) requires driver-specific references (e.g. `Npgsql`, `Microsoft.Data.SqlClient`, `MySqlConnector`, `Oracle.ManagedDataAccess.Core`, `Microsoft.Data.Sqlite`). Forcing all ADO.NET drivers onto every application bloats dependency graphs and binary sizes.
- **Decision**: Segregate each database provider into its own standalone package (`EricksonLopez.Concurrency.PostgreSql`, `EricksonLopez.Concurrency.SqlServer`, etc.) referencing only `Abstractions` and the specific ADO.NET driver.
- **Consequences**: Applications only reference their target database driver; minimal dependency footprint; zero transitive driver bloat.

---

## ADR-007: Domain Invariant Compare-And-Swap (CAS) Mutation Boundaries

- **Status**: Accepted
- **Context**: In-memory domain aggregates require safe state transitions when subjected to concurrent actor calls or asynchronous batch processes.
- **Decision**: Provide `IConcurrencyController.ExecuteCasAsync<TEntity>` which validates version preconditions before applying the domain mutation delegate and monotonically advancing `Version.Next()`.
- **Consequences**: Predictable in-memory state transitions; prevention of Lost Updates before database persistence; clean integration with OpenTelemetry metrics.

### ADR-007 Addendum: ExecuteCasAsync Thread-Safety Boundary

`ExecuteCasAsync` is **safe for sequential use** within a single execution context (e.g., a single command handler invocation). It does **not** provide in-memory mutual exclusion for concurrent threads accessing the same entity instance simultaneously. If multiple threads may access the same entity instance concurrently (rare in typical CQRS/DDD scenarios), a `SemaphoreSlim` or equivalent must be applied at the application layer before calling `ExecuteCasAsync`. The ultimate source of truth for concurrency correctness remains the **database write path** (version-conditioned UPDATE).

---

## ADR-008: Explicit Exclusion of Automatic Retries, Distributed Locks, and ORM Coupling from Core Scope

- **Status**: Accepted
- **Context**: As concurrency frameworks evolve, there is constant pressure to add generic distributed locks, background retries, ORM extensions, event stream versioning, and saga coordination. Incorporating these features into the core package dilutes the library's focus, introduces heavy network dependencies, and creates architectural bloat.
- **Decision**: The following features are permanently out of scope for `EricksonLopez.Concurrency` core and abstractions:
  1. **Automatic Retry/Backoff Loops in Core**: Delegated entirely to resilience policies (`EricksonLopez.Resilience` / Polly).
  2. **Distributed Locks (Redis/Consul/ZooKeeper)**: Handled by dedicated distributed locking libraries.
  3. **Entity Framework Core DbContext Coupling in Core**: Core remains lightweight, Dapper-first, and zero-ORM.
  4. **Event Sourcing / Event Stream Versioning**: Handled by dedicated Event Sourcing engines (`Marten`, `NEventStore`, `EricksonLopez.EventSourcing`).
  5. **Conflict-free Replicated Data Types (CRDT)**: Out of scope for optimistic SQL concurrency.
  6. **Saga Concurrency Orchestration**: Handled by process managers (`EricksonLopez.Processes`).
- **Consequences**: Controlled API surface, zero dependency bloat, pristine architectural boundaries, and ultra-high reliability.

---

## ADR-009: Testability and Mock-Free Test Doubles via FakeConcurrencyController

- **Status**: Accepted
- **Context**: Unit testing domain command handlers that depend on `IConcurrencyController` traditionally required mocking frameworks (e.g. NSubstitute, Moq). Mocking value structs, generic delegates, and asynchronous CAS calls introduces test fragility and verbosity.
- **Decision**: Provide a dedicated `EricksonLopez.Concurrency.Testing` package containing `FakeConcurrencyController` and `ConcurrencyConflictBuilder`.
- **Consequences**: Developers can verify optimistic concurrency behavior, simulate conflicts, queue transient retries, and inspect invocation histories with zero mocking boilerplate and zero reflection.

---

## ADR-010: ASP.NET Core Integration, RFC 7807 ProblemDetails, and HTTP ETag Semantics

- **Status**: Accepted
- **Context**: Web APIs built with ASP.NET Core require standardized translation of optimistic concurrency conflicts into HTTP 409 Conflict status codes, RFC 7807 `ProblemDetails` response bodies, and HTTP `ETag` / `If-Match` precondition headers.
- **Decision**: Provide `EricksonLopez.Concurrency.AspNetCore` with:
  1. `ConcurrencyConflictMiddleware`: Automatically catches `ConcurrencyException` and writes RFC 7807 HTTP 409 Conflict responses.
  2. `ConcurrencyProblemDetails`: Strongly typed RFC 7807 ProblemDetails model capturing conflict metadata.
  3. Minimal API `IResult` extensions: `Results.Extensions.ConcurrencyConflict(conflict)`.
  4. HTTP header extensions: `GetExpectedConcurrencyToken()`, `GetExpectedConcurrencyVersion()`, and `SetConcurrencyETag()`.
- **Consequences**: Turnkey REST API integration with 1 line of middleware registration; 100% Native AOT trimming safe.

---

## ADR-011: Conflict Resolution Strategy Lifecycle and RefreshAndRetry Resolver Pattern

- **Status**: Accepted
- **Context**: When optimistic concurrency conflicts occur, some domain applications need to automatically reload the latest persistent state, reconcile differences, and retry the operation.
- **Decision**: Provide `RefreshAndRetryConflictResolver<TEntity>` implementing `IConcurrencyConflictResolver<TEntity>`. The resolver verifies that the conflict classification is retryable, invokes a storage re-fetch delegate, optionally applies a custom domain merge delegate, and outputs `ConflictResolutionStrategy.RefreshAndRetry`. Non-retryable or fatal conflicts are rejected immediately.
- **Consequences**: Predictable conflict reconciliation; domain-level control over re-evaluation; clean isolation between database queries and domain logic.

---

## ADR-012: Zero-Allocation String and Span-Based Version Parsing Protocols

- **Status**: Accepted
- **Context**: Numeric version numbers frequently arrive from HTTP headers (`If-Match`), query parameters, route variables, or distributed messages as strings or character spans. Parsing them using standard `long.Parse` requires manual validation and throws expensive exceptions on malformed input.
- **Decision**: Implement `ISpanParsable<T>` and `IParsable<T>` on `ConcurrencyVersion` and `ConcurrencyVersion<TEntity>`, exposing zero-allocation `TryParse(ReadOnlySpan<char>, ...)` and `TryParse(string, ...)` methods.
- **Consequences**: High-performance, non-allocating version validation across web endpoints and message consumers.
