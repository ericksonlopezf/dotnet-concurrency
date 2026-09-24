# Technical API Reference (Microsoft Learn Style)

Complete and exhaustive technical reference for the **`EricksonLopez.Concurrency`** ecosystem structured according to **Microsoft Learn** technical documentation standards.

---

## 📑 Reference Index

1. [EricksonLopez.Concurrency.Abstractions](#1-ericksonlopezconcurrencyabstractions)
   - [ConcurrencyVersion](#concurrencyversion-struct)
   - [ExpectedVersion](#expectedversion-struct)
   - [ActualVersion](#actualversion-struct)
   - [ConcurrencyToken](#concurrencytoken-struct)
   - [CasResult&lt;TEntity&gt;](#casresulttentity-struct)
   - [ConcurrencyConflict](#concurrencyconflict-record)
   - [IConcurrencyController](#iconcurrencycontroller-interface)
   - [IMutableVersionedEntity](#imutableversionedentity-interface)
   - [IConcurrencyChecker](#iconcurrencychecker-interface)
   - [IConcurrencyConflictResolver&lt;TEntity&gt;](#iconcurrencyconflictresolvertentity-interface)
2. [EricksonLopez.Concurrency (Core)](#2-ericksonlopezconcurrency-core)
   - [ConcurrencyController](#concurrencycontroller-class)
   - [OptimisticConcurrencyChecker](#optimisticconcurrencychecker-class)
   - [ConcurrencyOptions](#concurrencyoptions-class)
   - [ConcurrencyDiagnostics](#concurrencydiagnostics-class)
   - [Core Conflict Resolvers](#core-conflict-resolvers)
3. [EricksonLopez.Concurrency.Dapper](#3-ericksonlopezconcurrencydapper)
   - [ConcurrencyDapperExtensions](#concurrencydapperextensions-class)
   - [OptimisticUpdateBuilder](#optimisticupdatebuilder-class)
4. [EricksonLopez.Concurrency.Result](#4-ericksonlopezconcurrencyresult)
   - [ConcurrencyErrors](#concurrencyerrors-class)
   - [ConcurrencyResultExtensions](#concurrencyresultextensions-class)
5. [EricksonLopez.Concurrency.Mediator](#5-ericksonlopezconcurrencymediator)
   - [ConcurrencyBehavior&lt;TRequest, TResponse&gt;](#concurrencybehaviortrequest-tresponse-class)
6. [EricksonLopez.Concurrency.Testing](#6-ericksonlopezconcurrencytesting)
   - [FakeConcurrencyController](#fakeconcurrencycontroller-class)
   - [ConcurrencyConflictBuilder](#concurrencyconflictbuilder-class)
7. [EricksonLopez.Concurrency.AspNetCore](#7-ericksonlopezconcurrencyaspnetcore)
   - [ConcurrencyConflictMiddleware](#concurrencyconflictmiddleware-class)
   - [ConcurrencyProblemDetails](#concurrencyproblemdetails-class)
   - [ConcurrencyHttpExtensions](#concurrencyhttpextensions-class)
8. [Database Dialects](#8-database-dialects)
   - [PostgreSql: PostgreSqlConcurrencyErrorClassifier & XminConcurrencyToken](#postgresql-classifier--token)
   - [SqlServer: SqlServerErrorClassifier & SqlServerRowVersionToken](#sqlserver-classifier--token)
   - [MySQL & MariaDB: Classifiers and Lock Extensions](#mysql--mariadb)
   - [Oracle: OracleConcurrencyErrorClassifier & OracleRowScnToken](#oracle-classifier--token)
   - [Sqlite: SqliteConcurrencyErrorClassifier](#sqlite-classifier)

---

## 1. `EricksonLopez.Concurrency.Abstractions`

### `ConcurrencyVersion` (Struct)

Represents an immutable 64-bit monotonic numeric counter for optimistic concurrency control, providing zero heap allocations and `ISpanParsable` / `ISpanFormattable` support.

#### Definition
```csharp
namespace EricksonLopez.Concurrency.Abstractions;

public readonly record struct ConcurrencyVersion : 
    IComparable<ConcurrencyVersion>, 
    IComparable, 
    ISpanFormattable, 
    ISpanParsable<ConcurrencyVersion>, 
    IParsable<ConcurrencyVersion>,
    IEquatable<ConcurrencyVersion>
```

#### Properties
- **`long Value { get; }`**: The underlying 64-bit integer value.
- **`bool IsNone { get; }`**: Returns `true` if the version value is 0 (uninitialized).

#### Key Methods
##### `Next()`
```csharp
public ConcurrencyVersion Next()
```
- **Returns**: A new `ConcurrencyVersion` instance with value `checked(Value + 1)`.
- **Exceptions**: `OverflowException` if the increment exceeds `long.MaxValue`.
- **Remarks**: Pure arithmetic transition with overflow protection.

##### `TryParse(ReadOnlySpan<char>, IFormatProvider?, out ConcurrencyVersion)`
```csharp
public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ConcurrencyVersion result)
```
- **Parameters**:
  - `s`: Character span to parse.
  - `provider`: Optional format provider.
  - `result`: Parsed `ConcurrencyVersion` instance upon success.
- **Returns**: `true` if the string represents a valid 64-bit integer; otherwise `false`.
- **Performance**: 0 bytes allocated on the managed heap.

#### When to Use
- Primary version identifier for domain entities and aggregate roots (`IVersionedEntity`).
#### When NOT to Use
- Public REST endpoints where exposing internal database sequence counters is undesirable (use opaque `ConcurrencyToken` instead).

---

### `ConcurrencyVersion<TEntity>` (Struct)

Phantom-typed specialization of `ConcurrencyVersion` bound to a specific entity type `TEntity`, providing compile-time safety against accidental cross-entity version misuse.

#### Definition
```csharp
namespace EricksonLopez.Concurrency.Abstractions;

public readonly record struct ConcurrencyVersion<TEntity> :
    IComparable<ConcurrencyVersion<TEntity>>,
    IComparable,
    ISpanFormattable,
    ISpanParsable<ConcurrencyVersion<TEntity>>,
    IParsable<ConcurrencyVersion<TEntity>>
```

#### Constructors
```csharp
// From scalar
public ConcurrencyVersion<TEntity>(long value)

// Bridge from untyped ConcurrencyVersion
public ConcurrencyVersion<TEntity>(ConcurrencyVersion version)
```

#### Key Methods

##### `Next()`
```csharp
public ConcurrencyVersion<TEntity> Next()
```
- **Returns**: A new `ConcurrencyVersion<TEntity>` incremented by 1 (overflow-checked).
- **Exceptions**: `OverflowException` if increment exceeds `long.MaxValue`.

##### `ToUntyped()`
```csharp
public ConcurrencyVersion ToUntyped()
```
- **Returns**: The equivalent untyped `ConcurrencyVersion` with the same numeric value.

#### When to Use
- Expose on entity aggregate roots via `IVersionedEntity<TEntity>.TypedVersion` to prevent version values from being accidentally applied to the wrong entity type at compile time.

#### When NOT to Use
- Persistence layer bindings: Dapper and raw SQL parameters require untyped `long`. Use `ToUntyped().Value` or the implicit `long` conversion.

---

### `ExpectedVersion` (Struct)

Expresses the version precondition that persisted state must satisfy before a write operation is authorized.

#### Definition
```csharp
namespace EricksonLopez.Concurrency.Abstractions;

public readonly record struct ExpectedVersion : 
    IComparable<ExpectedVersion>, 
    IComparable,
    IEquatable<ExpectedVersion>
```

#### Special Values
- **`ExpectedVersion.Any`**: Bypasses version validation (allows any state).
- **`ExpectedVersion.New`**: Requires the entity to be newly created (version 0 or non-existent).
- **`ExpectedVersion.Exists`**: Requires the entity to already exist in persistence (version > 0).

#### Key Methods
##### `Specific(long)` / `Specific(ConcurrencyVersion)`
```csharp
public static ExpectedVersion Specific(long version)
public static ExpectedVersion Specific(ConcurrencyVersion version)
```
- **Returns**: Precondition requiring the exact specified version.

##### `Matches(ConcurrencyVersion)`
```csharp
public bool Matches(ConcurrencyVersion actual)
```
- **Returns**: `true` if the actual version satisfies the precondition; otherwise `false`.

#### Example
```csharp
var expected = ExpectedVersion.Specific(3);
bool valid = expected.Matches(new ConcurrencyVersion(3)); // true
```

---

### `ConcurrencyToken` (Struct)

Represents an immutable opaque concurrency token (GUID, ETag, cryptographic hash, or binary rowversion).

#### Definition
```csharp
namespace EricksonLopez.Concurrency.Abstractions;

public readonly record struct ConcurrencyToken : 
    IConcurrencyToken, 
    IComparable<ConcurrencyToken>, 
    IComparable,
    IEquatable<ConcurrencyToken>
```

#### Key Methods
- `static ConcurrencyToken NewGuid()`: Generates a new random GUID token.
- `static ConcurrencyToken From(string value, string kind = "String")`: Instantiates from a string representation.
- `static ConcurrencyToken From(byte[] bytes)`: Instantiates from a binary byte array.

---

### `CasResult<TEntity>` (Struct)

Encapsulates the atomic outcome of a Compare-And-Swap operation.

#### Properties
- **`bool IsSuccess { get; }`**: `true` if the mutation was successfully applied and version advanced. Use `!IsSuccess` to detect conflicts.
- **`TEntity? Entity { get; }`**: The mutated entity upon success (non-null when `IsSuccess == true`).
- **`ConcurrencyVersion? NewVersion { get; }`**: The newly advanced version upon success (non-null when `IsSuccess == true`).
- **`ConcurrencyConflict? Conflict { get; }`**: Conflict descriptor upon collision (non-null when `IsSuccess == false`).

#### Example
```csharp
CasResult<Order> result = await controller.ExecuteCasAsync(order, ExpectedVersion.Specific(1), order.Id,
    (o, ct) => ValueTask.FromResult(o with { Status = "Confirmed" }));

if (!result.IsSuccess)
{
    // result.Conflict contains full diagnostic data
    return Result.Failure(ConcurrencyErrors.FromConflict(result.Conflict!));
}

// result.Entity and result.NewVersion are available here
```

---

### `ConcurrencyConflict` (Record)

Immutable record storing comprehensive diagnostic metadata for a concurrency collision.

#### Properties
```csharp
public string EntityId { get; init; }
public string EntityType { get; init; }
public ExpectedVersion? ExpectedVersion { get; init; }
public ActualVersion? ActualVersion { get; init; }
public IConcurrencyToken? ExpectedToken { get; init; }
public IConcurrencyToken? ActualToken { get; init; }
public ConcurrencyConflictType ConflictType { get; init; }
public ConcurrencyConflictClassification Classification { get; init; }
public string Operation { get; init; }
public string Message { get; init; }
public DateTimeOffset Timestamp { get; init; }
public IReadOnlyDictionary<string, string> Metadata { get; init; }
```

#### Factory Methods
- `static ConcurrencyConflict VersionMismatch(string entityId, string entityType, ExpectedVersion expected, ActualVersion actual, ...)`
- `static ConcurrencyConflict TokenMismatch(string entityId, string entityType, IConcurrencyToken expected, IConcurrencyToken actual, ...)`
- `static ConcurrencyConflict Deleted(string entityId, string entityType, string operation, ...)`

---

### `IConcurrencyController` (Interface)

Primary orchestrator contract for optimistic validation, in-memory mutual exclusion, and Compare-And-Swap (CAS) state transitions.

#### Methods
##### `VerifyVersion<TEntity>`
```csharp
ConcurrencyConflict? VerifyVersion<TEntity>(
    TEntity entity, 
    ExpectedVersion expected, 
    string entityId) 
    where TEntity : class, IVersionedEntity;
```
- **Returns**: `null` if no conflict exists; a populated `ConcurrencyConflict` if versions diverge.

##### `ExecuteCasAsync<TEntity>` (Asynchronous Mutation)
```csharp
ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
    TEntity entity, 
    ExpectedVersion expected, 
    string entityId, 
    Func<TEntity, CancellationToken, ValueTask<TEntity>> mutate, 
    CancellationToken ct = default) 
    where TEntity : class, IVersionedEntity;
```

##### `ExecuteCasAsync<TEntity>` (Synchronous Mutation Overload)
```csharp
ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
    TEntity entity, 
    ExpectedVersion expected, 
    string entityId, 
    Func<TEntity, CancellationToken, TEntity> mutate, 
    CancellationToken ct = default) 
    where TEntity : class, IVersionedEntity;
```
- **Remarks**:
  - Enforces per-entity mutual exclusion in memory using striped semaphores.
  - Reentrant invocations on the same `entityId` within the same async context throw `InvalidOperationException`.
  - Acquisition of the in-memory lock exceeding `DefaultMaxAcquisitionTimeout` throws `TimeoutException`.
  - Automatically advances `IMutableVersionedEntity.Version` upon success; for immutable entities, asserts that the delegate advanced the version.

---

### `IMutableVersionedEntity` (Interface)

Contract extending `IVersionedEntity` for entities whose version property can be directly set by concurrency controllers.

```csharp
namespace EricksonLopez.Concurrency.Abstractions;

public interface IMutableVersionedEntity : IVersionedEntity
{
    new long Version { get; set; }
}
```

---

## 2. `EricksonLopez.Concurrency` (Core)

### `ConcurrencyController` (Class)

Production implementation of `IConcurrencyController`, `IDisposable`, and `IAsyncDisposable` with OpenTelemetry instrumentation and pooled striped locks.

#### Best Practices
- Register as `Singleton` or `Scoped` via `services.AddEricksonLopezConcurrency()`.
- Use `ExecuteCasAsync` to coordinate concurrent aggregate modifications in memory prior to persistence.

---

### `OptimisticConcurrencyChecker` (Class)

Stateless, thread-safe singleton implementing `IConcurrencyChecker`.

#### Methods
```csharp
public static OptimisticConcurrencyChecker Instance { get; }

public bool CheckVersion(
    ExpectedVersion expected, 
    ConcurrencyVersion actual, 
    string entityId, 
    string entityType, 
    out ConcurrencyConflict? conflict);
```
- **Performance**: ~1.14 nanoseconds per check, 0 bytes allocated on the heap.

---

### `ConcurrencyOptions` (Class)

```csharp
public sealed class ConcurrencyOptions
{
    public ConflictResolutionStrategy DefaultResolutionStrategy { get; set; } = ConflictResolutionStrategy.Reject;
    public bool EnableDiagnostics { get; set; } = true;
    public ConcurrencyConflictClassification DefaultConflictClassification { get; set; } = ConcurrencyConflictClassification.Transient;
    public bool RecordDetailedActivityTags { get; set; } = true;
    public bool ThrowOnUnresolvedConflict { get; set; } = false;
    /// <summary>Maximum time to wait for in-memory lock acquisition (default: 5s). Exceeding this throws TimeoutException.</summary>
    public TimeSpan DefaultMaxAcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Maximum time allocated for the mutation delegate execution (default: 10s). Exceeding this cancels the delegate.</summary>
    public TimeSpan DefaultMaxExecutionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>Number of internal striped lock partitions. Defaults to max(256, Environment.ProcessorCount * 8). Raise for high-core-count machines.</summary>
    public int StripeCount { get; set; } = Math.Max(256, Environment.ProcessorCount * 8);
}
```

#### Property Details

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultResolutionStrategy` | `ConflictResolutionStrategy` | `Reject` | Strategy applied by registered `IConcurrencyConflictResolver<T>` |
| `EnableDiagnostics` | `bool` | `true` | Activates OpenTelemetry `ActivitySource` and `Meter` instrumentation |
| `DefaultConflictClassification` | `ConcurrencyConflictClassification` | `Transient` | Classification applied to generated conflicts |
| `RecordDetailedActivityTags` | `bool` | `true` | Emits entity ID, type, and version as Activity tags (careful with PII) |
| `ThrowOnUnresolvedConflict` | `bool` | `false` | Throws `ConcurrencyException` when no resolver handles the conflict |
| `DefaultMaxAcquisitionTimeout` | `TimeSpan` | `5s` | Lock-wait ceiling before `TimeoutException`. Prevents deadlocks |
| `DefaultMaxExecutionTimeout` | `TimeSpan` | `10s` | Mutation-delegate ceiling before cancellation. Prevents thread-pool starvation |
| `StripeCount` | `int` | `max(256, CPU * 8)` | Internal lock shard count. Tune upward for high-core-count or high-concurrency scenarios |

---

### Core Conflict Resolvers
- `RejectConflictResolver<TEntity>`: Rejects conflicts immediately, returning `ConflictResolution.Rejected(...)`.
- `LastWriteWinsConflictResolver<TEntity>`: Overwrites persisted state with the proposed entity without merge.
- `DelegateConflictResolver<TEntity>`: Delegates reconciliation to a developer-provided lambda `Func<TEntity, TEntity, ConcurrencyConflict, ValueTask<ConflictResolution<TEntity>>>`.
- `RefreshAndRetryConflictResolver<TEntity>`: Reloads latest state from persistence and reapplies mutation. Supports configurable retry count and backoff.

#### `RefreshAndRetryConflictResolver<TEntity>` — Constructors and Properties

```csharp
// Constructor 1: Fixed delay (or no delay) between retries
public RefreshAndRetryConflictResolver<TEntity>(
    Func<string, CancellationToken, ValueTask<TEntity?>> refreshDelegate,
    int maxRetries = 3);

// Constructor 2: Custom exponential or jittered backoff
public RefreshAndRetryConflictResolver<TEntity>(
    Func<string, CancellationToken, ValueTask<TEntity?>> refreshDelegate,
    int maxRetries,
    Func<int, TimeSpan> retryDelayProvider);
```

| Property | Type | Description |
|---|---|---|
| `MaxRetries` | `int` | Maximum number of reload-and-reapply cycles before escalating to `Rejected` |
| `RetryDelayProvider` | `Func<int, TimeSpan>?` | Optional function receiving 1-based attempt number, returning delay. `null` = no delay |

---

## 3. `EricksonLopez.Concurrency.Dapper`

### `OptimisticUpdateBuilder` (Class)

Safe SQL query builder for optimistic conditional updates with multi-tenant partitioning.

```csharp
public static string BuildVersionedUpdate(
    string tableName, 
    string setClauses, 
    string idColumn = "id", 
    string versionColumn = "version", 
    string idParam = "Id", 
    string versionParam = "ExpectedVersion", 
    string? tenantColumn = null, 
    string? tenantParam = null);
```

---

### `ConcurrencyDapperExtensions` (Class)

```csharp
public static async Task<ConcurrencyConflict?> ExecuteOptimisticAsync(
    this IDbConnection connection, 
    string sql, 
    object? param, 
    ExpectedVersion expectedVersion, 
    string entityId, 
    string entityType, 
    IDbTransaction? transaction = null, 
    int? commandTimeout = null, 
    CancellationToken cancellationToken = default);
```
- **Returns**: `null` if `rowsAffected > 0`; otherwise `ConcurrencyConflict.VersionMismatch`.

---

## 4. `EricksonLopez.Concurrency.Result`

### `ConcurrencyErrors` (Class)

Static class exposing canonical error code constants and factory methods for creating typed `EricksonLopez.Result.Error` values from concurrency failures.

#### Constants

| Constant | Value | Description |
|---|---|---|
| `ConcurrencyConflictCode` | `"Concurrency.Conflict"` | Base error code for any concurrency conflict |
| `VersionMismatchCode` | `"Concurrency.VersionMismatch"` | Optimistic version divergence |
| `TokenMismatchCode` | `"Concurrency.TokenMismatch"` | Opaque ETag / token divergence |
| `EntityDeletedCode` | `"Concurrency.EntityDeleted"` | Entity was concurrently deleted |
| `SerializationFailureCode` | `"Concurrency.SerializationFailure"` | Serialization isolation anomaly (PostgreSQL `40001`) |
| `DeadlockCode` | `"Concurrency.Deadlock"` | Database deadlock detected |

#### Factory Methods

```csharp
// Creates an Error with VersionMismatchCode and Retryable retryability
public static Error VersionMismatch(
    string entityId, string entityType,
    ExpectedVersion expected, ActualVersion actual);

// Creates an Error with TokenMismatchCode and Retryable retryability
public static Error TokenMismatch(
    string entityId, string entityType,
    IConcurrencyToken expected, IConcurrencyToken actual);
```

#### When to Use
- Use constants as error code discriminators in pattern-matched `Result.Error` handlers.
- Use factory methods when manually constructing `EricksonLopez.Result.Error` from conflict data without a `ConcurrencyConflict` instance.

---

### `ConcurrencyResultExtensions` (Class)

Functional interoperability extensions for `EricksonLopez.Result`.

```csharp
public static Result ToResult(this ConcurrencyConflict? conflict);
public static Result<TEntity> ToResult<TEntity>(this CasResult<TEntity> casResult);
public static Result<TEntity> ToResult<TEntity>(this ConflictResolution<TEntity> resolution);
public static Result FromRowsAffected(int rowsAffected, string entityId, string entityType, ExpectedVersion expected);
```

---

## 5. `EricksonLopez.Concurrency.Mediator`

### `ConcurrencyBehavior<TRequest, TResponse>` (Class)

CQRS pipeline behavior for `EricksonLopez.Mediator` observing command execution and tracking OpenTelemetry spans:

```csharp
public sealed class ConcurrencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
```

---

## 6. `EricksonLopez.Concurrency.Testing`

### `FakeConcurrencyController` (Class)

High-fidelity test double for `IConcurrencyController` with programmable outcomes:

| Method | Description |
|---|---|
| `WithSuccess(long? nextVersion = null)` | Configures **permanent** success for all subsequent calls |
| `WithConflict(ConcurrencyConflict conflict)` | Configures **permanent** conflict for all subsequent calls |
| `WithConflictOnNextWrite(ConcurrencyConflict conflict)` | Configures a **one-time** conflict (queue-based) |
| `WithConflictOnNextWrite(ConcurrencyConflictType type, string entityId, string entityType, ConcurrencyConflictClassification classification)` | Synthesizes and enqueues a one-time conflict from component parts |
| `WithSuccessOnNextWrite(long? nextVersion = null)` | Configures a **one-time** success with an explicit version (queue-based) |
| `WhenVerifyVersion(Func<...> callback)` | Registers a delegate-based override for `VerifyVersion` |
| `WhenVerifyToken(Func<...> callback)` | Registers a delegate-based override for `VerifyToken` |
| `Reset()` | Clears all queued outcomes and invocation history |

#### Observable State
- `TotalInvocations`: Total number of calls received across all methods.
- `VerifyVersionInvocations`: Recorded `VerifyVersion` call history.
- `ExecuteCasInvocations`: Recorded `ExecuteCasAsync` call history.

---

## 7. `EricksonLopez.Concurrency.AspNetCore`

### `ConcurrencyConflictMiddleware` (Class)

Middleware catching unhandled `ConcurrencyException` and emitting RFC 7807 / RFC 9457 HTTP 409 responses with updated `ETag`.

---

## 8. Database Dialects

### PostgreSQL (Classifier & Token)
- **`PostgreSqlConcurrencyErrorClassifier`**: Classifies SQLSTATE `40001` (Serialization), `40P01` (Deadlock), and `55P03` (Lock unavailable).
- **`XminConcurrencyToken`**: Zero-allocation token wrapping the native `xmin` 32-bit transaction ID.
- **`PostgreSqlLockExtensions.WithLock(PostgreSqlLockMode)`**: Appends one of the following lock clauses:

| `PostgreSqlLockMode` | Generated SQL Clause | Description |
|---|---|---|
| `ForUpdate` | `FOR UPDATE` | Exclusive row lock |
| `ForUpdateNowait` | `FOR UPDATE NOWAIT` | Exclusive lock, fail immediately if unavailable |
| `ForUpdateSkipLocked` | `FOR UPDATE SKIP LOCKED` | Skip locked rows (queue processing) |
| `ForShare` | `FOR SHARE` | Shared lock preventing concurrent writes |
| `ForNoKeyUpdate` | `FOR NO KEY UPDATE` | Exclusive lock allowing concurrent foreign-key reference reads |

### SQL Server (Classifier & Token)
- **`SqlServerErrorClassifier`**: Classifies SQL Server errors 1205 (Deadlock), 3960 (Snapshot conflict), and 1222 (Lock timeout).
- **`SqlServerRowVersionToken`**: 8-byte struct for `ROWVERSION` / `TIMESTAMP` columns.
- **`SqlServerLockExtensions.WithSqlServerTableHint`**: Injects `WITH (UPDLOCK, ROWLOCK)`.

### MySQL & MariaDB
- **`MySqlConcurrencyErrorClassifier` / `MariaDbConcurrencyErrorClassifier`**: Classifies errors 1213 and 1205.
- **`WithMariaDbLockWait(seconds)`**: Generates `FOR UPDATE WAIT n`.

### Oracle (Classifier & Token)
- **`OracleConcurrencyErrorClassifier`**: Classifies ORA-00060 (Deadlock), ORA-00054 (Resource busy), and ORA-08177.
- **`OracleRowScnToken`**: Token struct wrapping the 64-bit `ORA_ROWSCN` pseudo-column.
- **`WithOracleLockWait(seconds)`**: Generates `FOR UPDATE WAIT n`.

### SQLite (Classifier)
- **`SqliteConcurrencyErrorClassifier`**: Classifies `SQLITE_BUSY` (5) and `SQLITE_LOCKED` (6) as `Transient`.
