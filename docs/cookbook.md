# Concurrency Cookbook: Technical Engineering Recipes

A comprehensive collection of production engineering recipes for solving optimistic concurrency control, state synchronization, lost update prevention, and collision arbitration challenges using **`EricksonLopez.Concurrency`**.

---

## 📑 Recipe Index

1. [Recipe 1: REST API Concurrency Validation via ETags and If-Match](#recipe-1-rest-api-concurrency-validation-via-etags-and-if-match)
2. [Recipe 2: Zero-Roundtrip Atomic Updates with Dapper](#recipe-2-zero-roundtrip-atomic-updates-with-dapper)
3. [Recipe 3: In-Memory Atomic Mutations via Compare-And-Swap (CAS)](#recipe-3-in-memory-atomic-mutations-via-compare-and-swap-cas)
4. [Recipe 4: Automated Conflict Reconciliation with Domain Merging](#recipe-4-automated-conflict-reconciliation-with-domain-merging)
5. [Recipe 5: PostgreSQL Error Classification and Result Monadic Mapping](#recipe-5-postgresql-error-classification-and-result-monadic-mapping)
6. [Recipe 6: CQRS Pipeline with Mediator and ConcurrencyBehavior](#recipe-6-cqrs-pipeline-with-mediator-and-concurrencybehavior)
7. [Recipe 7: Multi-Tenant SQL Isolation in Optimistic Updates](#recipe-7-multi-tenant-sql-isolation-in-optimistic-updates)
8. [Recipe 8: State Synchronization with SQL Server ROWVERSION Tokens](#recipe-8-state-synchronization-with-sql-server-rowversion-tokens)
9. [Recipe 9: Unit Testing with FakeConcurrencyController and ConcurrencyConflictBuilder](#recipe-9-unit-testing-with-fakeconcurrencycontroller-and-concurrencyconflictbuilder)
10. [Recipe 10: HTTP 409 Conflict Handling with RFC 7807 ProblemDetails in ASP.NET Core](#recipe-10-http-409-conflict-handling-with-rfc-7807-problemdetails-in-aspnet-core)
11. [Recipe 11: State Reload and Reapplication with RefreshAndRetryConflictResolver](#recipe-11-state-reload-and-reapplication-with-refreshandretryconflictresolver)
12. [Recipe 12: MySQL and MariaDB Error and Timeout Classification](#recipe-12-mysql-and-mariadb-error-and-timeout-classification)
13. [Recipe 13: Oracle ORA_ROWSCN Tokens and Lock Waits](#recipe-13-oracle-ora_rowscn-tokens-and-lock-waits)
14. [Recipe 14: SQLite Database Locked and Busy Handling](#recipe-14-sqlite-database-locked-and-busy-handling)

---

## Recipe 1: REST API Concurrency Validation via ETags and If-Match

### Problem
Two web clients fetch the same resource (user profile) concurrently and submit independent modifications. Without optimistic validation, the second write silently overwrites the changes made by the first (the lost update anomaly).

### Solution
Use `ConcurrencyToken` to generate an HTTP `ETag` response header on `GET` requests and enforce the `If-Match` header on `PUT`/`PATCH` mutations. Validate token equality using `IConcurrencyController.VerifyToken` or `IConcurrencyChecker.CheckToken`.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Result;

public sealed class UserProfile : IConcurrencyAware
{
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IConcurrencyToken ConcurrencyToken { get; set; } = ConcurrencyToken.NewGuid();
}

public sealed class UserProfileService
{
    private readonly IConcurrencyController _concurrencyController;

    public UserProfileService(IConcurrencyController concurrencyController)
    {
        _concurrencyController = concurrencyController;
    }

    public Result<UserProfile> UpdateProfile(
        UserProfile profile,
        string newDisplayName,
        string incomingETag)
    {
        IConcurrencyToken expectedToken = ConcurrencyToken.From(incomingETag.Trim('"'), "Guid");

        ConcurrencyConflict? conflict = _concurrencyController.VerifyToken(
            entity: profile,
            expected: expectedToken,
            entityId: profile.UserId);

        if (conflict is not null)
        {
            // Maps directly to a conflict error with HTTP status 412 / 409
            return Result<UserProfile>.Failure(ConcurrencyErrors.FromConflict(conflict));
        }

        profile.DisplayName = newDisplayName;
        profile.ConcurrencyToken = ConcurrencyToken.NewGuid(); // Rotate token

        return Result<UserProfile>.Success(profile);
    }
}
```

### Explanation
1. The web client receives the resource with header `ETag: "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"`.
2. When submitting a mutation, the client includes `If-Match: "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"`.
3. `VerifyToken` compares the expected token against the current aggregate token in memory or database.
4. If tokens differ (indicating another client has already updated the aggregate), a `ConcurrencyConflict` of type `TokenMismatch` with `StaleState` classification is returned.

### Best Practices
- Always rotate the `ConcurrencyToken` via `ConcurrencyToken.NewGuid()` after every successful write.
- Strip surrounding quotes from the `If-Match` header before instantiating `ConcurrencyToken.From`.
- Return the new token in the HTTP 200 OK `ETag` response header.

### Common Pitfalls
- Forgetting to rotate the token upon save, allowing subsequent stale requests to succeed.
- Performing raw string comparisons without normalizing quotes (`"` vs `W/"`).

---

## Recipe 2: Zero-Roundtrip Atomic Updates with Dapper

### Problem
Executing a `SELECT version FROM table` query prior to an `UPDATE` statement creates a TOCTOU (Time-Of-Check to Time-Of-Use) race window and adds an unnecessary network round-trip.

### Solution
Execute a single parameterized SQL statement `UPDATE ... SET version = version + 1 WHERE id = @Id AND version = @ExpectedVersion` and evaluate whether `rowsAffected > 0` using `connection.ExecuteOptimisticAsync`.

### Complete Code
```csharp
using System.Data;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Dapper;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Result;

public sealed class ProductRepository
{
    private readonly IDbConnection _connection;

    public ProductRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Result> UpdatePriceAsync(
        string productId,
        decimal newPrice,
        ExpectedVersion expectedVersion)
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "products",
            setClauses: "price = @NewPrice",
            idColumn: "id",
            versionColumn: "version",
            idParam: "Id",
            versionParam: "ExpectedVersion");

        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticAsync(
            sql: sql,
            param: new
            {
                Id = productId,
                NewPrice = newPrice,
                ExpectedVersion = (long)expectedVersion.Version
            },
            expectedVersion: expectedVersion,
            entityId: productId,
            entityType: "Product");

        return conflict.ToResult();
    }
}
```

### Explanation
1. `OptimisticUpdateBuilder` generates an atomic SQL statement that modifies the row only if its current database version matches `@ExpectedVersion`.
2. The database engine acquires an exclusive row lock only for the microsecond duration of the write.
3. If another process modified the row first, `version` has advanced and the `WHERE` clause matches 0 rows, returning `rowsAffected = 0`.
4. `ExecuteOptimisticAsync` immediately converts `0` affected rows into a `ConcurrencyConflict.VersionMismatch`.

### Best Practices
- Avoid executing a prior `SELECT` query purely to check the version before writing.
- Always use `OptimisticUpdateBuilder` to guarantee consistent column and parameter names.
- Return `conflict.ToResult()` to propagate the outcome functionally without throwing exceptions.

### Common Pitfalls
- Forgetting to increment the version in the `SET` clause (`version = version + 1`).
- Using `<=` comparisons instead of strict `=` on expected versions.

---

## Recipe 3: In-Memory Atomic Mutations via Compare-And-Swap (CAS)

### Problem
Managing hot in-memory state (actors, cache entries, in-memory inventory buffers) requires atomic mutations that fail safely if another thread modifies state concurrently.

### Solution
Use `IConcurrencyController.ExecuteCasAsync` with a mutation delegate executed under atomic protection and version validation.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;

public sealed class InventoryStock : IVersionedEntity
{
    public string Sku { get; init; } = string.Empty;
    public int Available { get; set; }
    public long Version { get; set; }
}

public async Task<CasResult<InventoryStock>> DeductStockAsync(
    IConcurrencyController controller,
    InventoryStock currentStock,
    int quantityToDeduct)
{
    return await controller.ExecuteCasAsync(
        entity: currentStock,
        expected: ExpectedVersion.Specific(currentStock.Version),
        entityId: currentStock.Sku,
        mutate: (stock, cancellationToken) =>
        {
            if (stock.Available < quantityToDeduct)
            {
                throw new InvalidOperationException("Insufficient stock available.");
            }

            stock.Available -= quantityToDeduct;
            return ValueTask.FromResult(stock);
        });
}
```

### Explanation
1. `ExecuteCasAsync` acquires striped mutual exclusion for the aggregate `entityId` using a pooled lock.
2. Checks whether `currentStock.Version` matches `expected`.
3. If matched, executes the `mutate` delegate.
4. Upon mutation completion, advances version via `checked(Version + 1)` and returns `CasResult<T>.Succeeded`.
5. If mismatched, releases immediately and returns `CasResult<T>.Conflicted` without executing the delegate.

### Best Practices
- Keep the `mutate` delegate purely in-memory, free of network I/O or blocking operations.
- Inspect `casResult.IsSuccess` to decide whether to persist the state change or retry.

### Common Pitfalls
- Capturing outer mutable variables inside the `mutate` delegate.
- Forgetting that `IMutableVersionedEntity` enables in-place mutation without allocating wrapper structures.

---

## Recipe 4: Automated Conflict Reconciliation with Domain Merging

### Problem
When a concurrency conflict occurs, discarding user changes or aborting the transaction may be unacceptable for additive business workflows (such as balance deposits or appending items to an order).

### Solution
Implement `IConcurrencyConflictResolver<TEntity>` using `DelegateConflictResolver` or a custom class returning `ConflictResolution.Merged`.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Resolvers;

public sealed class BankAccount : IVersionedEntity
{
    public string AccountId { get; init; } = string.Empty;
    public decimal Balance { get; set; }
    public long Version { get; set; }
}

public sealed class AccountMergeConflictResolver : IConcurrencyConflictResolver<BankAccount>
{
    public ValueTask<ConflictResolution<BankAccount>> ResolveAsync(
        BankAccount proposed,
        BankAccount? currentDatabase,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        if (currentDatabase is null)
        {
            return ValueTask.FromResult(ConflictResolution.Rejected<BankAccount>("Account was deleted."));
        }

        // Domain merge: calculate proposed delta and apply it to current database balance
        decimal depositDelta = proposed.Balance - 1000m; // Added amount
        var merged = new BankAccount
        {
            AccountId = proposed.AccountId,
            Balance = currentDatabase.Balance + depositDelta,
            Version = currentDatabase.Version + 1
        };

        return ValueTask.FromResult(ConflictResolution.Merged(merged, "Deposit reconciled against latest balance."));
    }
}
```

### Explanation
1. When collision occurs on write, the resolver receives both proposed state (`proposed`) and the fresh database state (`currentDatabase`).
2. Semantic business differences are computed (deltas).
3. A new aggregate is produced with incremented version and reconciliation justification.

### Best Practices
- Apply merge resolutions only on commutative or purely additive operations.
- Record a detailed explanation in `ConflictResolution.Merged(entity, reason)`.

### Common Pitfalls
- Merging non-commutative fields (such as conflicting delivery address changes or cancellation states).
- Omitting null checks on `currentDatabase` (the entity might have been deleted concurrently).

---

## Recipe 5: PostgreSQL Error Classification and Result Monadic Mapping

### Problem
PostgreSQL throws `PostgresException` with raw SQLSTATE codes when serialization failures (`40001`) or deadlocks (`40P01`) occur under `SERIALIZABLE` or `REPEATABLE READ` transaction isolation.

### Solution
Use `PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict` to automatically classify these exceptions into a structured `ConcurrencyConflict` and convert it into an `Error.Conflict`.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.PostgreSql;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Result;
using Npgsql;

public async Task<Result<TResponse>> ExecuteTransactionalAsync<TResponse>(
    Func<Task<TResponse>> operation,
    string entityId,
    string entityType)
{
    try
    {
        TResponse result = await operation();
        return Result<TResponse>.Success(result);
    }
    catch (PostgresException pgEx)
    {
        ConcurrencyConflict? conflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(
            pgEx,
            entityId: entityId,
            entityType: entityType,
            operation: "ExecuteTransactional");

        if (conflict is not null)
        {
            Error error = ConcurrencyErrors.FromConflict(conflict);
            return Result<TResponse>.Failure(error);
        }

        throw; // Non-concurrency database exception
    }
}
```

### Explanation
1. `PostgreSqlConcurrencyErrorClassifier` inspects the `SqlState` property of `PostgresException`.
2. `40001` translates to `ConcurrencyConflictType.SerializationFailure` and `Transient` classification.
3. `40P01` translates to `ConcurrencyConflictType.Deadlock` and `Transient` classification.
4. `ConcurrencyErrors.FromConflict` produces a monadic error with SQLSTATE metadata and `Retryable` retryability.

### Best Practices
- Specifically catch `PostgresException` and rethrow if it is not a concurrency-related failure.
- Inspect `error.Retryability` in upstream resilience policies for exponential backoff retries.

### Common Pitfalls
- Mistaking unique constraint violations (`23505`) for transient concurrency conflicts.
- Swallowing non-concurrency exceptions (syntax errors, connection dropouts).

---

## Recipe 6: CQRS Pipeline with Mediator and ConcurrencyBehavior

### Problem
Enforcing concurrency preconditions, opening OpenTelemetry traces, and recording metrics on every CQRS command produces boilerplate across all command handlers.

### Solution
Implement `IConcurrencyAwareRequest<TResponse>` on the command and register `AddConcurrencyMediatorBehavior()` in the dependency injection container.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Concurrency.Mediator.DependencyInjection;
using EricksonLopez.Mediator;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

public sealed record TransferFundsCommand(
    string SourceAccountId,
    string TargetAccountId,
    decimal Amount,
    ExpectedVersion ExpectedVersion) : IConcurrencyAwareRequest<Result<bool>>
{
    ExpectedVersion? IConcurrencyAwareRequest.ExpectedVersion => ExpectedVersion;
}

public static void ConfigureServices(IServiceCollection services)
{
    services.AddEricksonLopezConcurrency();
    services.AddConcurrencyMediatorBehavior();
    services.AddMediator();
}
```

### Explanation
1. `ConcurrencyBehavior` intercepts the incoming request before reaching the handler.
2. Starts an OpenTelemetry `Activity` with command name and expected version.
3. Executes the command pipeline and automatically emits duration metrics and success/conflict telemetry counters.

### Best Practices
- Implement explicit interface member `IConcurrencyAwareRequest.ExpectedVersion` on the command record.
- Call `AddConcurrencyMediatorBehavior()` before registering application-specific handlers.

### Common Pitfalls
- Returning result types incompatible with `Result<T>` when expecting monadic error handling.

---

## Recipe 7: Multi-Tenant SQL Isolation in Optimistic Updates

### Problem
In multi-tenant SaaS applications, an omission in update predicates can lead to cross-tenant data corruption or unauthorized overwrites.

### Solution
Use `OptimisticUpdateBuilder.BuildVersionedUpdate` specifying both `tenantColumn` and `tenantParam`.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Dapper;

string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
    tableName: "tenant_documents",
    setClauses: "title = @Title, content = @Content",
    idColumn: "document_id",
    versionColumn: "version",
    idParam: "DocumentId",
    versionParam: "ExpectedVersion",
    tenantColumn: "tenant_id",
    tenantParam: "TenantId");

// Generates:
// UPDATE tenant_documents 
// SET title = @Title, content = @Content, version = version + 1
// WHERE document_id = @DocumentId AND tenant_id = @TenantId AND version = @ExpectedVersion;
```

### Explanation
1. The generated `WHERE` clause combines primary key, tenant identifier, and expected version.
2. If an attacker or corrupted context sends another tenant's `id`, the statement matches 0 rows and fails safely without corrupting data.

### Best Practices
- Always enforce a composite index `(tenant_id, document_id)` on the underlying database table.
- Resolve `TenantId` strictly from the authenticated security principal, never from untrusted client payloads.

### Common Pitfalls
- Omitting the `tenantColumn` argument, relying only on primary key uniqueness.

---

## Recipe 8: State Synchronization with SQL Server ROWVERSION Tokens

### Problem
Microsoft SQL Server maintains automatic 8-byte binary `ROWVERSION` / `TIMESTAMP` columns updated on every physical row write.

### Solution
Use `SqlServerRowVersionToken` to parse, compare, and serialize binary rowversion tokens with zero heap allocations.

### Complete Code
```csharp
using EricksonLopez.Concurrency.SqlServer;

// 1. Parse from hex string (e.g. received from an API or DTO)
var token = SqlServerRowVersionToken.Parse("0x00000000000007D1");

// 2. Extract byte array for ADO.NET / Dapper parameterization
byte[] binaryBytes = token.ToByteArray();

// 3. Perform high-performance in-memory comparison
var currentDbToken = new SqlServerRowVersionToken(binaryBytes);
bool matches = token.Equals(currentDbToken); // true
```

### Explanation
1. `SqlServerRowVersionToken` is an immutable struct holding the 8 bytes internally.
2. Implements `IConcurrencyToken` and `IComparable<SqlServerRowVersionToken>`, enabling direct equality checks without string conversions.

### Best Practices
- Define the SQL Server column as `rowversion NOT NULL`.
- Use `token.ToByteArray()` to parameterize Dapper queries.

### Common Pitfalls
- Treating `ROWVERSION` as a sequential row counter across different tables (it is monotonic only database-wide).

---

## Recipe 9: Unit Testing with FakeConcurrencyController and ConcurrencyConflictBuilder

### Problem
Unit testing command handlers requires simulating concurrency collisions without spinning up real databases or configuring fragile reflection mocks.

### Solution
Use `FakeConcurrencyController` and `ConcurrencyConflictBuilder` from `EricksonLopez.Concurrency.Testing`.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Testing;
using Xunit;

public class AccountCommandHandlerTests
{
    [Fact]
    public void Handler_WhenConflictOccurs_ReturnsFailure()
    {
        // 1. Configure test double
        var fakeController = new FakeConcurrencyController();
        
        ConcurrencyConflict simulatedConflict = new ConcurrencyConflictBuilder()
            .WithEntityId("ACC-101")
            .WithEntityType("Account")
            .WithConflictType(ConcurrencyConflictType.VersionMismatch)
            .WithClassification(ConcurrencyConflictClassification.Transient)
            .WithVersions(ExpectedVersion.Specific(1), ActualVersion.From(2))
            .Build();

        fakeController.WithConflictOnNextWrite(simulatedConflict);

        // 2. Act
        var account = new BankAccount("ACC-101", "Bob", 100m, 1);
        ConcurrencyConflict? conflict = fakeController.VerifyVersion(account, ExpectedVersion.Specific(1), account.AccountId);

        // 3. Assert
        Assert.NotNull(conflict);
        Assert.Equal(1, fakeController.TotalInvocations);
        Assert.Single(fakeController.VerifyVersionInvocations);
    }
}
```

### Explanation
1. `FakeConcurrencyController` implements `IConcurrencyController` and tracks every invocation in read-only audit collections (`VerifyVersionInvocations`, `ExecuteCasInvocations`).
2. `WithConflictOnNextWrite` simulates exactly one collision on the next write, allowing retry policy testing.

### Best Practices
- Call `fakeController.Reset()` between tests if the instance is reused across test cases.
- Assert `fakeController.TotalInvocations` to verify that code does not execute redundant checks.

### Common Pitfalls
- Using permanent `WithConflict` when testing successful recovery following a single retry.

---

## Recipe 10: HTTP 409 Conflict Handling with RFC 7807 ProblemDetails in ASP.NET Core

### Problem
Web APIs require standardized RFC 7807 / RFC 9457 responses with HTTP status 409 Conflict and structured metadata when collisions occur.

### Solution
Configure `ConcurrencyConflictMiddleware` and `ConcurrencyProblemDetails` in ASP.NET Core.

### Complete Code
```csharp
using EricksonLopez.Concurrency.AspNetCore.DependencyInjection;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using EricksonLopez.Concurrency.AspNetCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEricksonLopezConcurrency();
builder.Services.AddConcurrencyAspNetCore();

var app = builder.Build();

// Enable automated ConcurrencyException capture
app.UseConcurrencyConflictHandling();

// Or return explicitly in Minimal APIs
app.MapPut("/api/v1/accounts/{id}", (string id, HttpRequest request, BankAccount updated) =>
{
    long? expectedVersion = request.GetExpectedConcurrencyVersion();
    
    // Simulate collision:
    var conflict = ConcurrencyConflict.VersionMismatch(
        id, 
        "BankAccount", 
        ExpectedVersion.Specific(expectedVersion ?? 1), 
        ActualVersion.From(2));
    
    return Results.Extensions.ConcurrencyConflict(conflict, request.Path);
});
```

### Explanation
1. `UseConcurrencyConflictHandling()` registers middleware catching any unhandled `ConcurrencyException`.
2. Converts the exception into an RFC 7807 payload with Content-Type `application/problem+json` and HTTP 409 status.

### Best Practices
- Extract versions and tokens from request headers using `request.GetExpectedConcurrencyVersion()`.
- Return `Results.Extensions.ConcurrencyConflict` in Minimal APIs.

### Common Pitfalls
- Returning HTTP 500 Internal Server Error upon concurrency conflict instead of 409 Conflict or 412 Precondition Failed.

---

## Recipe 11: State Reload and Reapplication with RefreshAndRetryConflictResolver

### Problem
In high-contention systems, resolving a collision requires reloading the latest persisted state from storage and reapplying business mutation logic over fresh data.

### Solution
Use `RefreshAndRetryConflictResolver<TEntity>` configuring the reload and reapplication delegates.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Resolvers;

var resolver = new RefreshAndRetryConflictResolver<BankAccount>(
    refreshDelegate: async (entityId, ct) =>
    {
        // Reload fresh state from storage
        return await database.LoadAccountAsync(entityId, ct);
    },
    reapplyDelegate: (proposed, fresh) =>
    {
        // Reapply deposit differential over newly reloaded balance
        decimal delta = proposed.Balance - 1000m;
        return new BankAccount(fresh.AccountId, fresh.Owner, fresh.Balance + delta, fresh.Version + 1);
    },
    maxRetries: 3);

ConflictResolution<BankAccount> outcome = await resolver.ResolveAsync(localProposed, null, conflict);
```

### Explanation
1. When conflict is detected, `RefreshAndRetryConflictResolver` invokes `refreshDelegate` to fetch the latest persisted record.
2. Executes `reapplyDelegate` combining original intent with fresh entity state.
3. Returns `ConflictResolution.RefreshedAndRetried` with the advanced version.

### Best Practices
- Cap the maximum retry count via `maxRetries` (typically 3).
- Cancel operation gracefully if `CancellationToken` is signaled during reload.

### Common Pitfalls
- Reapplying non-idempotent mutations without recalculating differentials.

---

## Recipe 12: MySQL and MariaDB Error and Timeout Classification

### Problem
In MySQL and MariaDB, concurrent transactions under `REPEATABLE READ` isolation can trigger deadlocks (error 1213) or lock wait timeouts (error 1205).

### Solution
Use `MySqlConcurrencyErrorClassifier` or `MariaDbConcurrencyErrorClassifier` and append `WithMariaDbLockWait(seconds)` clauses.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.MariaDb;
using MySqlConnector;

string query = "SELECT id, balance FROM accounts WHERE id = @Id".WithMariaDbLockWait(5);

try
{
    // Execute query with bounded 5-second lock wait
    await connection.ExecuteAsync(query, new { Id = "ACC-501" });
}
catch (MySqlException ex)
{
    ConcurrencyConflict? conflict = MariaDbConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "ACC-501", "Account");
    if (conflict is not null && conflict.Classification == ConcurrencyConflictClassification.Transient)
    {
        // Eligible for backoff retry
    }
}
```

### Explanation
1. `WithMariaDbLockWait(5)` appends SQL clause `FOR UPDATE WAIT 5`.
2. If the lock is not granted within 5 seconds, MariaDB throws error 1205.
3. The classifier catches the timeout and categorizes it as `Transient`, making it eligible for retry.

### Best Practices
- Avoid indefinite lock waits in pessimistic row-locking queries.
- Use `FOR UPDATE NOWAIT` in MySQL (`MySqlLockMode.ForUpdateNoWait`) to fail immediately when a resource is held.

### Common Pitfalls
- Assuming all MySqlConnector exceptions are transient without passing them through the classifier.

---

## Recipe 13: Oracle ORA_ROWSCN Tokens and Lock Waits

### Problem
Oracle Database maintains the `ORA_ROWSCN` pseudo-column reflecting the system change number (SCN) without requiring custom version columns.

### Solution
Use `OracleRowScnToken`, `OracleLockExtensions`, and `OracleConcurrencyErrorClassifier`.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Oracle;

// 1. Generate query with 10-second bounded lock wait
string oracleSql = "SELECT id, balance FROM accounts WHERE id = @Id".WithOracleLockWait(10);

// 2. Parse and compare SCN retrieved from ORA_ROWSCN
var scnToken = OracleRowScnToken.Parse("184467440737");
Console.WriteLine($"Token Oracle SCN: {scnToken.Value}, Kind: {scnToken.TokenKind}");
```

### Explanation
1. `OracleRowScnToken` encapsulates the 64-bit Oracle SCN and satisfies `IConcurrencyToken`.
2. `WithOracleLockWait(10)` generates the standard `FOR UPDATE WAIT 10` clause.

### Best Practices
- Enable `ROWDEPENDENCIES` when creating Oracle tables so that `ORA_ROWSCN` tracks row-level rather than data-block-level changes.

### Common Pitfalls
- Using `ORA_ROWSCN` on tables created with `NOROWDEPENDENCIES` (the SCN changes for all rows sharing the same physical data block).

---

## Recipe 14: SQLite Database Locked and Busy Handling

### Problem
SQLite is an embedded single-writer database engine; concurrent write operations result in `SQLITE_BUSY` (code 5) or `SQLITE_LOCKED` (code 6) errors.

### Solution
Use `SqliteConcurrencyErrorClassifier` to inspect `SqliteException` and evaluate whether the failure is transient.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Sqlite;
using Microsoft.Data.Sqlite;

try
{
    await sqliteCommand.ExecuteNonQueryAsync();
}
catch (SqliteException ex)
{
    if (SqliteConcurrencyErrorClassifier.IsBusy(ex) || SqliteConcurrencyErrorClassifier.IsLocked(ex))
    {
        ConcurrencyConflict? conflict = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "RECORD-1", "Document");
        // Classified as Transient -> Ready for jittered retry
    }
}
```

### Explanation
1. `SqliteConcurrencyErrorClassifier` inspects `SqliteErrorCode`.
2. Codes 5 and 6 are classified as `Transient`, as the lock releases as soon as the active write transaction commits.

### Best Practices
- Configure `PRAGMA journal_mode = WAL;` and a sufficient `busy_timeout` in the SQLite connection string.
- Always classify with `SqliteConcurrencyErrorClassifier` to avoid confusing `SQLITE_BUSY` with `SQLITE_CONSTRAINT` integrity errors.

### Common Pitfalls
- Retrying immediately in SQLite without randomized jitter or delay, exacerbating database file contention.

