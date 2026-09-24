# Frequently Asked Questions (FAQ)

Architectural rationale, design decisions, and technical answers about **`EricksonLopez.Concurrency`**.

---

## 1. Foundational Concepts & Architecture

### What problem does `EricksonLopez.Concurrency` solve?
It eliminates **Lost Updates (silent overwrites)** and **Time-Of-Check to Time-Of-Use (TOCTOU)** race conditions in distributed systems, microservices, and high-throughput RESTful APIs. It guarantees that when multiple processes attempt to mutate the same record concurrently, exactly one succeeds and competing updates are deterministically detected, classified, and arbitrated without corrupting data.

### Why Optimistic Concurrency Control (OCC) instead of Distributed Locks (Redis Redlock, Consul, Mutexes)?
1. **Throughput & Low Latency**: OCC assumes write collisions are infrequent under typical workloads. It avoids pre-write network roundtrips, delivering sub-nanosecond in-memory comparisons and millions of checks per second.
2. **Zero Infrastructure Dependencies**: It requires no external Redis, Consul, or ZooKeeper clusters, eliminating operational complexity and single points of failure.
3. **Immunity to GC Pauses & Clock Drift**: Lease-based distributed locks are vulnerable to long Garbage Collection pauses exceeding lease TTLs, which can cause two nodes to believe they hold the lock simultaneously. In OCC, atomicity is guaranteed at the persistence engine transaction boundary.

---

## 2. In-Memory CAS & ConcurrencyController

### How does `ExecuteCasAsync` provide mutual exclusion without global bottlenecks?
`ConcurrencyController` implements striped, fine-grained entity locks using a pool of `RefCountedLock` instances. Each entity ID hashes to a dedicated lock stripe, isolating independent entities and preventing lock contention across disparate records. Furthermore, it tracks active locks via an `AsyncLocal` reentrancy node to detect and reject recursive calls on the same entity with an `InvalidOperationException`.

### How does in-memory CAS differ from database optimistic locking?
- **In-Memory CAS (`ExecuteCasAsync`)**: Protects domain entity instances held in process memory (such as long-lived actor states, domain aggregate instances, or in-memory caches) from concurrent mutation during a read-modify-write delegate execution.
- **Database OCC (`ExecuteOptimisticAsync`)**: Enforces atomic state transitions at the relational database level via parameterized single-statement `UPDATE ... WHERE version = @ExpectedVersion` clauses, serving as the ultimate ground truth across distributed application nodes.

---

## 3. Performance & Memory Model

### How is the Zero-Allocation (0 bytes on heap) guarantee achieved?
Foundational primitives (`ConcurrencyVersion`, `ExpectedVersion`, `ActualVersion`, `ConcurrencyToken`) are implemented as 64-bit stack-allocated `readonly record struct` value types. They produce 0 heap allocations during comparisons on hot paths (`OptimisticConcurrencyChecker`), entirely avoiding Gen 0/1 garbage collection overhead.

### Is the ecosystem 100% compatible with Native AOT on .NET 8, .NET 9, and .NET 10?
Yes. All packages enforce `<IsAotCompatible>true</IsAotCompatible>` and `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>`. The codebase uses zero dynamic runtime code generation (`Reflection.Emit`) and no unannotated reflection on execution paths, as validated by the automated `EricksonLopez.Concurrency.AotSmokeTest` suite in CI.

---

## 4. Comparison with ORMs

### How does it compare to Entity Framework Core concurrency tokens?
| Feature | Entity Framework Core | EricksonLopez.Concurrency |
|---|---|---|
| **Mechanism** | Change Tracker in memory, dynamic entity proxies | Zero-allocation value structs, Dapper, native CAS |
| **Allocation Profile** | High (entity tracking graph, snapshot allocations) | **0 bytes** on verification hot paths |
| **Latency** | Microseconds to milliseconds | Sub-nanosecond (~1.14 ns) comparisons |
| **Native AOT** | Complex configuration, partial trimming support | **100% Native AOT** by design |
| **Database Dialects** | Generic `DbUpdateConcurrencyException` | Granular error taxonomy for 6 engines (Postgres, SQL Server, MySQL, MariaDB, Oracle, SQLite) |

---

## 5. Integration & Resilience Boundaries

### Why does the library exclude automatic retry loops from core?
Per the **Single Responsibility Principle (SRP)** and **ADR-001**:
- `EricksonLopez.Concurrency` is strictly responsible for **detecting and classifying** conflicts (`Transient`, `StaleState`, `NonRetryable`).
- Orchestrating retry policies (exponential backoff, randomized jitter, circuit breaking) is delegated to **`EricksonLopez.Resilience`** or application-level use case orchestrators.
This clean boundary eliminates circular dependencies and allows developers to apply retries at the appropriate level (HTTP request, database transaction, or message queue consumer).

### How are multi-tenant race conditions isolated?
`OptimisticUpdateBuilder` natively supports compound tenant partition keys:
```sql
UPDATE tenant_accounts 
SET balance = @Balance, version = version + 1 
WHERE id = @Id AND tenant_id = @TenantId AND version = @ExpectedVersion;
```
If a request submits an update for an existing ID under an incorrect `tenant_id`, the statement affects 0 rows and fails safely as a concurrency conflict, preventing cross-tenant data leakage.
