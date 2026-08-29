# Public API Reference (Microsoft Learn Style)

Complete and exhaustive technical reference of the public API surface for **`EricksonLopez.Concurrency`**.

---

## 📦 Packages and Namespaces

| Package | Main Namespace | Description |
|---|---|---|
| `EricksonLopez.Concurrency.Abstractions` | `EricksonLopez.Concurrency.Abstractions` | Core contracts, zero-allocation value types, and conflict models. |
| `EricksonLopez.Concurrency` | `EricksonLopez.Concurrency.Controllers`, `.Diagnostics`, `.Resolvers`, `.DependencyInjection` | In-memory controller, checker, built-in resolvers, and OpenTelemetry instrumentation. |
| `EricksonLopez.Concurrency.Result` | `EricksonLopez.Concurrency.Result` | Functional extensions and monadic mapping to `Result<T>` and `Error`. |
| `EricksonLopez.Concurrency.Dapper` | `EricksonLopez.Concurrency.Dapper` | Conditional SQL write extensions and `OptimisticUpdateBuilder`. |
| `EricksonLopez.Concurrency.PostgreSql` | `EricksonLopez.Concurrency.PostgreSql` | PostgreSQL SQLSTATE classifier, `xmin` token, and `FOR UPDATE` lock helpers. |
| `EricksonLopez.Concurrency.SqlServer` | `EricksonLopez.Concurrency.SqlServer` | SQL Server error classifier, `ROWVERSION` token, and `UPDLOCK` table hints. |
| `EricksonLopez.Concurrency.MySql` | `EricksonLopez.Concurrency.MySql` | MySQL error classifier and locking helpers. |
| `EricksonLopez.Concurrency.MariaDb` | `EricksonLopez.Concurrency.MariaDb` | MariaDB error classifier and timed `FOR UPDATE WAIT` locking helpers. |
| `EricksonLopez.Concurrency.Oracle` | `EricksonLopez.Concurrency.Oracle` | Oracle ORA classifier, `ORA_ROWSCN` token, and timed locking helpers. |
| `EricksonLopez.Concurrency.Sqlite` | `EricksonLopez.Concurrency.Sqlite` | SQLite error classifier (`SQLITE_BUSY`, `SQLITE_LOCKED`). |
| `EricksonLopez.Concurrency.Mediator` | `EricksonLopez.Concurrency.Mediator` | Pipeline behavior for `IConcurrencyAwareRequest<T>` CQRS commands. |
| `EricksonLopez.Concurrency.Testing` | `EricksonLopez.Concurrency.Testing` | In-memory mock-free test doubles and fluent conflict test builders. |
| `EricksonLopez.Concurrency.AspNetCore` | `EricksonLopez.Concurrency.AspNetCore.Middleware`, `.Models`, `.Extensions` | RFC 7807 problem details middleware, HTTP ETag helpers, and minimal API results. |

---

## 🏷️ Zero-Allocation Value Types & Models (Abstractions)

### `ConcurrencyVersion` (readonly record struct)
Represents an immutable, zero-heap-allocation numeric version counter.
- **Declaration**: `public readonly record struct ConcurrencyVersion : IComparable<ConcurrencyVersion>, IComparable, ISpanFormattable, ISpanParsable<ConcurrencyVersion>, IParsable<ConcurrencyVersion>`
- **Static Fields**:
  - `ConcurrencyVersion.None`: Uninitialized version (value: 0).
  - `ConcurrencyVersion.Initial`: Initial version for newly created entities (value: 1).
- **Properties**:
  - `long Value { get; }`: The 64-bit integer version value.
  - `bool IsNone { get; }`: Returns `true` if the value is 0.
- **Methods**:
  - `ConcurrencyVersion Next()`: Returns the next consecutive version (`checked(Value + 1)`).
  - `static ConcurrencyVersion From(long value)`: Creates an instance from a `long`.
  - `static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ConcurrencyVersion result)`: Zero-allocation span parsing (implements `ISpanParsable<ConcurrencyVersion>`).
  - `static bool TryParse(string? s, IFormatProvider? provider, out ConcurrencyVersion result)`: String-based parsing (implements `IParsable<ConcurrencyVersion>`).
- **Conversions & Operators**:
  - Implicit conversion to `long`.
  - Explicit conversion from `long`.
  - Comparison operators `<`, `<=`, `>`, `>=`.
- **Performance**: Zero heap allocation; implements `ISpanFormattable` for allocation-free formatting and `ISpanParsable<ConcurrencyVersion>` for zero-allocation span-based parsing from HTTP headers, query strings, and distributed messages.

---

### `ExpectedVersion` (readonly record struct)
Represents a version pre-condition required before applying a state mutation.
- **Declaration**: `public readonly record struct ExpectedVersion : IComparable<ExpectedVersion>, IComparable`
- **Special Values**:
  - `ExpectedVersion.Any`: Matches any version (disables version validation).
  - `ExpectedVersion.New`: Expects the entity to be new / not yet persisted (Version 0).
  - `ExpectedVersion.Exists`: Expects the entity to exist (any Version > 0).
- **Methods**:
  - `static ExpectedVersion Specific(long version)`: Expects an exact numeric version.
  - `static ExpectedVersion Specific(ConcurrencyVersion version)`: Expects an exact typed version.
  - `bool Matches(ConcurrencyVersion actual)`: Evaluates whether the actual version satisfies the expectation.

---

### `ActualVersion` (readonly record struct)
Represents the actual version discovered in persistence or state cache.
- **Declaration**: `public readonly record struct ActualVersion : IComparable<ActualVersion>, IComparable`
- **Special Values**:
  - `ActualVersion.NotFound`: Indicates the entity was not found or deleted (`exists: false`, version: 0).
- **Properties**:
  - `ConcurrencyVersion Version { get; }`
  - `bool Exists { get; }`

---

### `ConcurrencyToken` (readonly record struct)
Represents an opaque immutable token used for state coherence validation (e.g., GUID, Hash, ETag).
- **Declaration**: `public readonly record struct ConcurrencyToken : IConcurrencyToken, IComparable<ConcurrencyToken>, IComparable`
- **Static Methods**:
  - `static ConcurrencyToken NewGuid()`: Generates a random GUID-based token.
  - `static ConcurrencyToken From(Guid tokenValue)`: Creates from a GUID.
  - `static ConcurrencyToken From(byte[] bytes)`: Creates from a binary array (hex format).
  - `static ConcurrencyToken From(string value, string kind = "String")`: Creates from a string and kind discriminator.
- **Properties**:
  - `string Value { get; }`: String representation of the token.
  - `string TokenKind { get; }`: Kind/format discriminator.
  - `bool IsEmpty { get; }`: Returns `true` if null or empty.

---

### `ConcurrencyConflict` (sealed record)
Encapsulates rich, immutable details of a detected concurrency collision.
- **Properties**:
  - `string EntityId { get; init; }`
  - `string EntityType { get; init; }`
  - `ExpectedVersion? ExpectedVersion { get; init; }`
  - `ActualVersion? ActualVersion { get; init; }`
  - `IConcurrencyToken? ExpectedToken { get; init; }`
  - `IConcurrencyToken? ActualToken { get; init; }`
  - `ConcurrencyConflictType ConflictType { get; init; }`
  - `ConcurrencyConflictClassification Classification { get; init; }`
  - `string Operation { get; init; }`
  - `string Message { get; init; }`
  - `DateTimeOffset Timestamp { get; init; }`
  - `IReadOnlyDictionary<string, string> Metadata { get; init; }`
- **Factory Methods**:
  - `static ConcurrencyConflict VersionMismatch(...)`
  - `static ConcurrencyConflict TokenMismatch(...)`
  - `static ConcurrencyConflict Deleted(...)`

---

### `ConcurrencyConflictClassification` (enum)
- `Transient = 0`: Transient database conflict (deadlock, serialization failure) eligible for immediate retry.
- `Retryable = 1`: Conflict resolvable by reloading state and re-evaluating domain invariants.
- `NonRetryable = 2`: Non-retryable state conflict requiring user intervention.
- `StaleState = 3`: Stale client state.
- `Fatal = 4`: Unrecoverable state corruption.

---

### `ConcurrencyConflictType` (enum)
- `VersionMismatch = 0`
- `TokenMismatch = 1`
- `StateDeleted = 2`
- `AlreadyExists = 3`
- `SerializationFailure = 4`
- `Deadlock = 5`
- `LockUnavailable = 6`
- `Custom = 7`

---

### `CasResult<TEntity>` (readonly record struct)
Represents the outcome of a Compare-And-Swap (`ExecuteCasAsync`) state transition.
- **Properties**:
  - `bool IsSuccess { get; }`: `true` if the CAS operation succeeded.
  - `bool IsConflict { get; }`: `true` if the operation was blocked by a version conflict.
  - `TEntity? Entity { get; }`: The mutated entity instance if successful; otherwise, `null`.
  - `ConcurrencyVersion? NewVersion { get; }`: The new concurrency version if successful; otherwise, `null`.
  - `ConcurrencyConflict? Conflict { get; }`: The conflict descriptor if conflicted; otherwise, `null`.
- **Factory Methods** (`CasResult` static class):
  - `static CasResult<TEntity> Succeeded<TEntity>(TEntity entity, ConcurrencyVersion newVersion)`: Creates a successful CAS result.
  - `static CasResult<TEntity> Conflicted<TEntity>(ConcurrencyConflict conflict)`: Creates a conflicted CAS result.

---

### `ConflictResolution<TEntity>` (readonly record struct) / `ConflictResolution` (static factory)
Encapsulates the outcome of an `IConcurrencyConflictResolver<TEntity>.ResolveAsync` call.
- **Properties** (on `ConflictResolution<TEntity>`):
  - `TEntity? Entity { get; }`: The reconciled entity state, or `null` if rejected.
  - `ConflictResolutionStrategy Strategy { get; }`: The resolution strategy applied.
  - `string Reason { get; }`: A human-readable explanation of the resolution outcome.
- **Factory Methods** (`ConflictResolution` static class):
  - `static ConflictResolution<TEntity> Rejected<TEntity>(string? reason = null)`: Creates a rejected resolution.
  - `static ConflictResolution<TEntity> Merged<TEntity>(TEntity mergedEntity, string? reason = null)`: Creates a domain-merged resolution.
  - `static ConflictResolution<TEntity> LastWriteWins<TEntity>(TEntity entity, string? reason = null)`: Creates an explicit Last-Write-Wins resolution.
  - `static ConflictResolution<TEntity> RefreshedAndRetried<TEntity>(TEntity refreshedEntity, string? reason = null)`: Creates a refresh-and-retry resolution.

---

### `IConcurrencyConflictResolver<TEntity>` (interface)
Defines the contract for domain-specific conflict resolution strategies.
- **Method**: `ValueTask<ConflictResolution<TEntity>> ResolveAsync(TEntity proposedEntity, TEntity? currentDatabaseEntity, ConcurrencyConflict conflict, CancellationToken cancellationToken = default)`
- **Built-in implementations** (in `EricksonLopez.Concurrency` Core package):
  - `RejectConflictResolver<TEntity>`: Always rejects (default registration).
  - `LastWriteWinsConflictResolver<TEntity>`: Always overwrites with the proposed entity.
  - `DelegateConflictResolver<TEntity>`: Delegates resolution to a user-provided function.
  - `RefreshAndRetryConflictResolver<TEntity>`: Reloads the latest state from storage and optionally re-applies domain mutations.

---

## 🛠️ Services and Controllers (Core)

### `IConcurrencyController` / `ConcurrencyController`
Main orchestrator for optimistic validations and atomic in-memory Compare-And-Swap (CAS) mutations.
- **Methods**:
  - `ConcurrencyConflict? VerifyVersion<TEntity>(TEntity entity, ExpectedVersion expected, string entityId) where TEntity : class, IVersionedEntity`: Validates entity version in memory.
  - `ConcurrencyConflict? VerifyToken<TEntity>(TEntity entity, IConcurrencyToken expected, string entityId) where TEntity : class, IConcurrencyAware`: Validates entity token.
  - `ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(TEntity entity, ExpectedVersion expected, string entityId, Func<TEntity, CancellationToken, ValueTask<TEntity>> mutate, CancellationToken ct = default)`: Executes an atomic in-memory CAS mutation.

---

### `IConcurrencyChecker` / `OptimisticConcurrencyChecker`
Zero-allocation synchronous evaluator for versions and tokens.
- **Methods**:
  - `bool CheckVersion(ExpectedVersion expected, ConcurrencyVersion actual, string entityId, string entityType, out ConcurrencyConflict? conflict)`
  - `bool CheckToken(IConcurrencyToken expected, IConcurrencyToken actual, string entityId, string entityType, out ConcurrencyConflict? conflict)`

---

### `ConcurrencyOptions`
Configuration class for the core concurrency framework, consumed by `AddEricksonLopezConcurrency(Action<ConcurrencyOptions>?)`.
- **Properties**:
  - `ConflictResolutionStrategy DefaultResolutionStrategy { get; set; }` *(default: `ConflictResolutionStrategy.Reject`)*: The default strategy applied when a concurrency conflict is detected.
  - `bool EnableDiagnostics { get; set; }` *(default: `true`)*: Enables or disables OpenTelemetry activity tracking and metrics.
  - `ConcurrencyConflictClassification DefaultConflictClassification { get; set; }` *(default: `ConcurrencyConflictClassification.Transient`)*: The default conflict classification assigned when generating generic version mismatch conflicts.
  - `bool RecordDetailedActivityTags { get; set; }` *(default: `true`)*: Whether detailed contextual tags are attached to OpenTelemetry activities.
  - `bool ThrowOnUnresolvedConflict { get; set; }` *(default: `false`)*: When `true`, automatically throws `ConcurrencyException` for unresolved conflicts instead of returning the conflict model to the caller.

---

### Dependency Injection Extensions

#### `ConcurrencyServiceCollectionExtensions`
- `AddEricksonLopezConcurrency(this IServiceCollection services, Action<ConcurrencyOptions>? configure = null)`: Registers the default concurrency checker (`OptimisticConcurrencyChecker`), controller (`ConcurrencyController`), rejection resolver (`RejectConflictResolver<T>`), and `ConcurrencyOptions`. All registrations use `TryAdd*` semantics for safe overriding.
- `AddConflictResolver<TEntity, TResolver>(this IServiceCollection services)`: Registers a custom domain-specific conflict resolver as `Scoped` for the specified entity type.

---

## 🗄️ Dapper Extensions

### `ConcurrencyDapperExtensions`
- `ExecuteOptimisticAsync(this IDbConnection connection, string sql, object? param, ExpectedVersion expectedVersion, string entityId, string entityType, ...)`: Executes SQL command and returns `null` if `rowsAffected > 0`, or a `ConcurrencyConflict.VersionMismatch` if `rowsAffected == 0`.
- `ExecuteOptimisticTokenAsync(...)`: Similar execution for concurrency tokens.

### `OptimisticUpdateBuilder`
- `BuildVersionedUpdate(string tableName, string setClauses, string idColumn = "id", string versionColumn = "version", string idParam = "Id", string versionParam = "ExpectedVersion", string? tenantColumn = null, string? tenantParam = null)`: Constructs standardized SQL `UPDATE ... SET version = version + 1 WHERE id = @Id [AND tenant_id = @TenantId] AND version = @ExpectedVersion`.

---

## 🌐 Database Dialects

### PostgreSQL (`EricksonLopez.Concurrency.PostgreSql`)
- `XminConcurrencyToken`: Token wrapping the system `xmin` column (uint32).
- `PostgreSqlConcurrencyErrorClassifier`:
  - `IsSerializationFailure(Exception)`: Detects SQLSTATE `40001`.
  - `IsDeadlock(Exception)`: Detects SQLSTATE `40P01`.
  - `IsLockNotAvailable(Exception)`: Detects SQLSTATE `55P03`.
  - `IsUniqueViolation(Exception)`: Detects SQLSTATE `23505`.
  - `IsTransient(Exception)`: Classifies if eligible for transaction retry.
  - `ToConcurrencyConflict(...)`: Maps to `ConcurrencyConflict`.
- `PostgreSqlLockExtensions.WithLock(this string sql, PostgreSqlLockMode mode)`: Appends `FOR UPDATE`, `NOWAIT`, `SKIP LOCKED`, `FOR SHARE`, `FOR NO KEY UPDATE`.

### SQL Server (`EricksonLopez.Concurrency.SqlServer`)
- `SqlServerRowVersionToken`: Token wrapping the 8-byte binary `ROWVERSION` / `TIMESTAMP` column.
- `SqlServerErrorClassifier`:
  - `IsDeadlock(Exception)`: Error 1205.
  - `IsSerializationFailure(Exception)`: Errors 3960 and 3961 (Snapshot isolation conflicts).
  - `IsLockTimeout(Exception)`: Error 1222.
  - `IsUniqueViolation(Exception)`: Errors 2601 and 2627.
- `SqlServerLockExtensions.WithSqlServerTableHint(this string tableName, SqlServerLockMode mode)`: Appends table hints `WITH (UPDLOCK, ROWLOCK)`, `WITH (XLOCK, ROWLOCK, NOWAIT)`, etc.

### MySQL & MariaDB
- `MySqlConcurrencyErrorClassifier` / `MariaDbConcurrencyErrorClassifier`: Errors 1213 (Deadlock), 1205 (Lock Timeout), 1062 (Duplicate Key).
- `WithMySqlLock`, `WithMariaDbLock`, `WithMariaDbLockWait(seconds)`.

### Oracle (`EricksonLopez.Concurrency.Oracle`)
- `OracleRowScnToken`: Token wrapping the `ORA_ROWSCN` pseudo-column (uint64 / long).
- `OracleConcurrencyErrorClassifier`: ORA-00060 (Deadlock), ORA-00054 (Resource Busy), ORA-08177 (Serialization Failure), ORA-00001 (Unique Constraint).
- `WithOracleLock`, `WithOracleLockWait(seconds)`.

### SQLite (`EricksonLopez.Concurrency.Sqlite`)
- `SqliteConcurrencyErrorClassifier`: Errors `SQLITE_BUSY` (5), `SQLITE_LOCKED` (6), `SQLITE_CONSTRAINT` (19).

---

## ⚡ Mediator Pipeline (`EricksonLopez.Concurrency.Mediator`)
- `IConcurrencyAwareRequest`: Marker interface for requests with concurrency constraints.
- `IConcurrencyAwareRequest<TResponse>`: Strongly typed contract for CQRS commands.
- `ConcurrencyBehavior<TRequest, TResponse>`: Pipeline behavior creating OpenTelemetry spans and recording metrics upon intercepting concurrent requests.
- `AddConcurrencyMediatorBehavior(this IServiceCollection services)`: Registers pipeline behavior in DI container.

---

## 🧪 Testing Utilities (`EricksonLopez.Concurrency.Testing`)
- `FakeConcurrencyController`: In-memory test double for `IConcurrencyController`.
  - `WithSuccess(long? nextVersion = null)`: Sets the controller to simulate successful executions.
  - `WithConflict(ConcurrencyConflict conflict)`: Sets a permanent conflict response.
  - `WithConflictOnNextWrite(...)`: Simulates a single transient conflict followed by success.
  - `Reset()`: Clears all call histories and resets behavior.
  - `VerifyVersionInvocations`, `VerifyTokenInvocations`, `ExecuteCasInvocations`: Read-only call records for assertions.
- `ConcurrencyConflictBuilder`: Fluent builder for constructing `ConcurrencyConflict` test instances with custom classifications, timestamps, and metadata.

---

## 🌐 ASP.NET Core Web API (`EricksonLopez.Concurrency.AspNetCore`)
- `ConcurrencyConflictMiddleware`: Automatically catches unhandled `ConcurrencyException` instances and writes RFC 7807 ProblemDetails with HTTP 409 and `ETag` headers.
- `ConcurrencyProblemDetails`: RFC 7807 ProblemDetails model containing entity ID, type, classification, and versions.
- `UseConcurrencyConflictHandling(this IApplicationBuilder app)`: Registers middleware in pipeline.
- `Results.Extensions.ConcurrencyConflict(this IResultExtensions, ConcurrencyConflict conflict, string? instance = null)`: Minimal API `IResult` factory.
- `GetExpectedConcurrencyToken(this HttpRequest request)`: Extracts expected token from `If-Match` / `If-None-Match`.
- `GetExpectedConcurrencyVersion(this HttpRequest request)`: Extracts expected numeric version from `If-Match` / `If-None-Match`.
- `SetConcurrencyETag(this HttpResponse response, IConcurrencyToken/ConcurrencyVersion token, bool isWeak = true)`: Formats and sets `ETag` header.

