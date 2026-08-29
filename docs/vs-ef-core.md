# Comparing EricksonLopez.Concurrency vs. Entity Framework Core

This document outlines the differences between `EricksonLopez.Concurrency` and the Entity Framework Core concurrency subsystem, helping teams migrating from EF Core to Dapper or evaluating lightweight concurrency options.

---

## 1. High-Level Comparison

| Feature | EF Core Concurrency | EricksonLopez.Concurrency |
|---|---|---|
| **ORM Requirement** | Requires full `DbContext` & entity change tracking | **Zero ORM** — Dapper-first, ADO.NET native |
| **Heap Allocations on Checks** | Multiple heap allocations per `SaveChanges()` | **0 bytes** (Value struct pipelines) |
| **Conflict Taxonomy** | Generic `DbUpdateConcurrencyException` | **8 granular conflict types** (`Deadlock`, `SerializationFailure`, `VersionMismatch`, `StateDeleted`, etc.) |
| **Retryability Classification** | None (Caller must parse provider inner exceptions) | **5 first-class classifications** (`Transient`, `Retryable`, `StaleState`, `NonRetryable`, `Fatal`) |
| **In-Memory Pre-Persistence CAS** | Not supported | **Built-in** `ExecuteCasAsync<TEntity>` |
| **Multi-Engine Native Tokens** | SQL Server `[Timestamp]`, PostgreSQL `xmin` | PostgreSQL `xmin`, SQL Server `RowVersion`, Oracle `ORA_ROWSCN`, MySQL, MariaDB, SQLite |
| **Native AOT Trimming** | Partial / complex configuration | **100% Native AOT Trimming Safe** (`TreatWarningsAsErrors=true`) |
| **OpenTelemetry Instrumentation** | General EF Core diagnostic listener | **Dedicated Concurrency Meters & Spans** (`concurrency.conflicts`, `concurrency.merges`, `concurrency.duration`) |

---

## 2. Code Equivalences & Migration Recipes

### 2.1 Concurrency Tokens & Versions

#### In Entity Framework Core:
```csharp
public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

// In repository / service:
try
{
    _dbContext.Orders.Update(order);
    await _dbContext.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Need to reload entry, compare values manually...
}
```

#### In EricksonLopez.Concurrency + Dapper:
```csharp
public class Order : IVersionedEntity
{
    public string Id { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public long Version { get; set; }
}

// In Dapper repository:
var (conflict, rows) = await connection.ExecuteOptimisticAsync(
    "UPDATE Orders SET Total = @Total, Version = Version + 1 WHERE Id = @Id AND Version = @ExpectedVersion",
    new { order.Id, order.Total, ExpectedVersion = order.Version },
    ExpectedVersion.Specific(order.Version),
    order.Id);

if (conflict is not null)
{
    // Programmatic conflict resolution:
    if (conflict.Classification == ConcurrencyConflictClassification.Transient)
    {
        // Safe to retry
    }
}
```

---

## 3. Why Choose EricksonLopez.Concurrency?

1. **Explicit over Implicit**: No background change tracker altering entity states invisibly.
2. **Actionable Conflict Diagnostics**: Know immediately whether the error was a transient database deadlock or a user stale write without parsing raw driver error codes.
3. **Engine Portability**: Identical concurrency programming model across 6 distinct database engines.
