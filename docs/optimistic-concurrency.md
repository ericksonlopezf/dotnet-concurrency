# Optimistic Concurrency Control

## 1. Fundamentals & Mechanism

Optimistic Concurrency Control (OCC) assumes that multiple transactions or operations can frequently complete without interfering with each other. Instead of acquiring heavy locks prior to reading or mutating data, the application checks whether another concurrent writer has updated the record during the operation lifetime.

In the `EricksonLopez` ecosystem, optimistic concurrency operates via two primary mechanisms:
1. **Numeric Version Counters**: `ConcurrencyVersion` and `ExpectedVersion`.
2. **Opaque Tokens**: `ConcurrencyToken` and `XminConcurrencyToken`.

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant App as Application Service
    participant Concurrency as OptimisticConcurrencyChecker
    participant DB as PostgreSQL Database

    Client->>App: UpdateProduct(id: "p1", expectedVersion: 5, newPrice: 120)
    App->>DB: SELECT * FROM products WHERE id = "p1"
    DB-->>App: Product(id: "p1", version: 5, price: 100)
    App->>Concurrency: CheckVersion(expected: 5, actual: 5)
    Concurrency-->>App: Valid (true, null)
    App->>DB: UPDATE products SET price = 120, version = 6 WHERE id = "p1" AND version = 5
    alt Successful update (1 row affected)
        DB-->>App: 1 row affected
        App-->>Client: Succeeded(version: 6)
    else Concurrent modification occurred (0 rows affected)
        DB-->>App: 0 rows affected
        App->>Concurrency: Create VersionMismatch conflict
        App-->>Client: Result.Failure(ErrorType.Conflict, "VersionMismatch")
    end
```

---

## 2. In-Memory vs In-Database Verification

| Feature | In-Memory Verification (`OptimisticConcurrencyChecker`) | In-Database Verification (`Dapper / PostgreSQL`) |
|---|---|---|
| **Location** | Application Domain / Memory | Database Transaction Engine |
| **Execution Point** | Before applying business mutations | Atomic with the `UPDATE` SQL execution |
| **Failure Cost** | 0 database writes, zero wasted persistence overhead | 0 rows affected, rollback of local transaction |
| **Race Protection** | Fast rejection of stale commands | True serialized write arbitration across distributed nodes |

---

## 3. Best Practices

- Always declare `IVersionedEntity` on domain aggregate roots requiring update tracking.
- Pass `ExpectedVersion` explicitly in command DTOs from clients or message consumers.
- Pair in-memory validation with database-level conditional writes (`WHERE version = @ExpectedVersion`) for complete defense-in-depth.
