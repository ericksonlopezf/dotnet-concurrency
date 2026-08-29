# Concurrency Cookbook: Technical Engineering Recipes

A curated collection of practical software engineering recipes for solving common concurrency control, state synchronization, and race condition problems using **`EricksonLopez.Concurrency`**.

---

## 📑 Recipe Index

1. [Recipe 1: REST API Concurrency Validation via ETags (If-Match)](#recipe-1-rest-api-concurrency-validation-via-etags-if-match)
2. [Recipe 2: Zero-Roundtrip Atomic Updates with Dapper](#recipe-2-zero-roundtrip-atomic-updates-with-dapper)
3. [Recipe 3: In-Memory Atomic Mutations via Compare-And-Swap (CAS)](#recipe-3-in-memory-atomic-mutations-via-compare-and-swap-cas)
4. [Recipe 4: Automated Conflict Reconciliation with Domain Merging](#recipe-4-automated-conflict-reconciliation-with-domain-merging)
5. [Recipe 5: PostgreSQL Error Classification & Monadic Result Mapping](#recipe-5-postgresql-error-classification--monadic-result-mapping)
6. [Recipe 6: CQRS Command Pipeline with Mediator and ConcurrencyBehavior](#recipe-6-cqrs-command-pipeline-with-mediator-and-concurrencybehavior)
7. [Recipe 7: Multi-Tenant SQL Isolation in Optimistic Updates](#recipe-7-multi-tenant-sql-isolation-in-optimistic-updates)
8. [Recipe 8: State Synchronization with SQL Server ROWVERSION Tokens](#recipe-8-state-synchronization-with-sql-server-rowversion-tokens)
9. [Recipe 9: Unit Testing Handlers with FakeConcurrencyController and ConcurrencyConflictBuilder](#recipe-9-unit-testing-handlers-with-fakeconcurrencycontroller-and-concurrencyconflictbuilder)
10. [Recipe 10: ASP.NET Core RFC 7807 409 Conflict Handling with ProblemDetails & Middleware](#recipe-10-aspnet-core-rfc-7807-409-conflict-handling-with-problemdetails--middleware)
11. [Recipe 11: Refresh and Re-apply State via RefreshAndRetryConflictResolver](#recipe-11-refresh-and-re-apply-state-via-refreshandretryconflictresolver)

---

## Recipe 1: REST API Concurrency Validation via ETags (If-Match)

### Problem
Two web clients fetch the same resource (user profile) and attempt to modify it simultaneously. Without optimistic validation, the second update silently overwrites the first (Lost Update anomaly).

### Solution
Use `ConcurrencyToken` to generate an HTTP `ETag` response header on `GET` requests and require an `If-Match` header on `PUT`/`PATCH` requests. Validate the match using `IConcurrencyController.VerifyToken`.

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
            // Returns Error with Conflict type (maps directly to HTTP 412 Precondition Failed)
            return Result<UserProfile>.Failure(ConcurrencyErrors.FromConflict(conflict));
        }

        profile.DisplayName = newDisplayName;
        profile.ConcurrencyToken = ConcurrencyToken.NewGuid(); // Rotate token on mutation

        return Result<UserProfile>.Success(profile);
    }
}
```

### Best Practices
- Rotate `ConcurrencyToken` using `ConcurrencyToken.NewGuid()` on every successful state change.
- Return the updated token in the `ETag: "..."` response header.

---

## Recipe 2: Zero-Roundtrip Atomic Updates with Dapper

### Problem
Performing a preceding `SELECT` query to fetch the current version followed by an `UPDATE` introduces a Time-Of-Check to Time-Of-Use (TOCTOU) race window and adds network latency.

### Solution
Execute a single parameterized SQL `UPDATE ... WHERE id = @Id AND version = @ExpectedVersion` statement and evaluate whether `rowsAffected > 0` using `connection.ExecuteOptimisticAsync`.

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

---

## Recipe 3: In-Memory Atomic Mutations via Compare-And-Swap (CAS)

### Problem
Managing hot in-memory state (such as actor state, cache entries, or real-time counters) requires atomic mutations that fail safely if another thread modifies the state concurrently.

### Solution
Use `IConcurrencyController.ExecuteCasAsync` with a mutation delegate that executes atomically.

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

---

## Recipe 4: Automated Conflict Reconciliation with Domain Merging

### Problem
When a concurrent write conflict occurs, discarding the user's changes or crashing may be unacceptable for business workflows where operations are additive (such as updating order quantities or tags).

### Solution
Implement `IConcurrencyConflictResolver<TEntity>` with `ConflictResolution.Merged` to reconcile local changes with updated database state.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;

public sealed class OrderAggregate : IVersionedEntity
{
    public string OrderId { get; init; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public long Version { get; set; }
}

public sealed class OrderConflictResolver : IConcurrencyConflictResolver<OrderAggregate>
{
    public ValueTask<ConflictResolution<OrderAggregate>> ResolveAsync(
        OrderAggregate proposed,
        OrderAggregate? currentDatabase,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        if (currentDatabase is null)
        {
            return ValueTask.FromResult(ConflictResolution.Rejected<OrderAggregate>("Order was deleted."));
        }

        // Domain reconciliation: compute diff and apply to latest database version
        decimal diff = proposed.TotalAmount - 100m;
        var merged = new OrderAggregate
        {
            OrderId = proposed.OrderId,
            TotalAmount = currentDatabase.TotalAmount + diff,
            Version = currentDatabase.Version + 1
        };

        return ValueTask.FromResult(ConflictResolution.Merged(merged, "Additions merged with latest database state."));
    }
}
```

---

## Recipe 5: PostgreSQL Error Classification & Monadic Result Mapping

### Problem
PostgreSQL throws `PostgresException` with SQLSTATE codes when serialization failures or deadlocks occur under `SERIALIZABLE` or `REPEATABLE READ` transaction isolation.

### Solution
Use `PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict` to detect transient serialization failures (`40001`) and deadlock errors (`40P01`) and convert them into `EricksonLopez.Result.Error`.

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

---

## Recipe 6: CQRS Command Pipeline with Mediator and ConcurrencyBehavior

### Problem
All write commands requiring optimistic concurrency checks should automatically validate version constraints, log OpenTelemetry spans, and increment metrics without boilerplate code duplicated in every command handler.

### Solution
Implement `IConcurrencyAwareRequest<TResponse>` on the command record and register `AddConcurrencyMediatorBehavior()` in the dependency injection container.

### Complete Code
```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Concurrency.Mediator.DependencyInjection;
using EricksonLopez.Mediator;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

// 1. Define command
public sealed record UpdateAccountBalanceCommand(
    string AccountId,
    decimal NewBalance,
    ExpectedVersion ExpectedVersion) : IConcurrencyAwareRequest<Result<bool>>
{
    ExpectedVersion? IConcurrencyAwareRequest.ExpectedVersion => ExpectedVersion;
}

// 2. Configure DI Container
public static void ConfigureServices(IServiceCollection services)
{
    services.AddEricksonLopezConcurrency();
    services.AddConcurrencyMediatorBehavior();
    services.AddMediator();
}
```

---

## Recipe 7: Multi-Tenant SQL Isolation in Optimistic Updates

### Problem
In multi-tenant SaaS applications, an optimistic update query must enforce tenant boundary isolation at the database level to prevent cross-tenant state mutation.

### Solution
Use `OptimisticUpdateBuilder.BuildVersionedUpdate` specifying `tenantColumn` and `tenantParam`.

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
// UPDATE tenant_documents SET title = @Title, content = @Content, version = version + 1
// WHERE document_id = @DocumentId AND tenant_id = @TenantId AND version = @ExpectedVersion;
```

---

## Recipe 8: State Synchronization with SQL Server ROWVERSION Tokens

### Problem
Microsoft SQL Server automatically maintains 8-byte binary `ROWVERSION` / `TIMESTAMP` columns that update on every row modification.

### Solution
Use `SqlServerRowVersionToken` to parse, compare, and serialize binary version tokens with zero allocations.

### Complete Code
```csharp
using EricksonLopez.Concurrency.SqlServer;

// Parse from incoming hex string (e.g., "0x00000000000007D1")
var token = SqlServerRowVersionToken.Parse("0x00000000000007D1");

// Extract byte array for Dapper / ADO.NET parameter
byte[] binaryValue = token.ToByteArray();

// Token comparison
var currentToken = new SqlServerRowVersionToken(binaryValue);
bool isMatch = token.Equals(currentToken); // true
```

---

## Recipe 9: Unit Testing Handlers with FakeConcurrencyController and ConcurrencyConflictBuilder

### Problem
Unit tests for command handlers need to simulate concurrency conflicts deterministically without spinning up physical databases or complex mock frameworks.

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
        // 1. Arrange test double
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

---

## Recipe 10: ASP.NET Core RFC 7807 409 Conflict Handling with ProblemDetails & Middleware

### Problem
RESTful APIs need standard RFC 7807 HTTP 409 Conflict problem details and ETag headers when an optimistic concurrency conflict occurs.

### Solution
Use `ConcurrencyConflictMiddleware` and `ConcurrencyProblemDetails` from `EricksonLopez.Concurrency.AspNetCore`.

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

// Enable automatic translation of ConcurrencyException to HTTP 409 ProblemDetails
app.UseConcurrencyConflictHandling();

// Or return explicitly in Minimal API endpoints
app.MapPut("/api/v1/accounts/{id}", (string id, HttpRequest request, BankAccount updated) =>
{
    // Extract version from If-Match
    long? expectedVersion = request.GetExpectedConcurrencyVersion();
    
    // If conflict detected:
    var conflict = ConcurrencyConflict.VersionMismatch(id, "BankAccount", ExpectedVersion.Specific(expectedVersion ?? 1), ActualVersion.From(2));
    
    return Results.Extensions.ConcurrencyConflict(conflict, request.Path);
});
```

---

## Recipe 11: Refresh and Re-apply State via RefreshAndRetryConflictResolver

### Problem
In high-throughput environments, a conflict might simply require reloading the latest entity state from the database and re-applying the domain mutation on top of fresh data.

### Solution
Use `RefreshAndRetryConflictResolver<TEntity>` specifying a reload delegate and a re-apply delegate.

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
        // Re-apply the deposit delta on top of the freshly reloaded balance
        decimal delta = proposed.Balance - 1000m;
        return new BankAccount(fresh.AccountId, fresh.Owner, fresh.Balance + delta, fresh.Version + 1);
    },
    maxRetries: 3);

ConflictResolution<BankAccount> outcome = await resolver.ResolveAsync(localProposed, null, conflict);
```
