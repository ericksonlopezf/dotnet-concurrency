# EricksonLopez.Concurrency — Architectural Standards & Design Rules

## 1. Clean Architecture & Layer Responsibilities

`EricksonLopez.Concurrency` strictly adheres to Clean Architecture layer isolation:

```
[ Domain / Abstractions ]
           ▲
           │
[ Application / Core & Result & Mediator ]
           ▲
           │
[ Infrastructure / Dapper & PostgreSQL ]
```

### Layer Permitted Matrix

| Layer | Permitted Types & Constructs | Strictly Prohibited |
|---|---|---|
| **Abstractions** | `IVersionedEntity`, `IConcurrencyToken`, `ConcurrencyVersion`, `ConcurrencyToken`, `ExpectedVersion`, `ActualVersion`, `ConcurrencyConflict`, `IConcurrencyChecker`, `IConcurrencyController`, Exceptions | References to Dapper, Npgsql, SQLite, HTTP headers, serialization attributes, reflection |
| **Core** | `OptimisticConcurrencyChecker`, `ConcurrencyController`, conflict resolvers (`Reject`, `LastWriteWins`, `Delegate`), `ConcurrencyDiagnostics`, options, DI extensions | Database drivers, SQL queries, ASP.NET Core controllers |
| **Result Adapter** | `ConcurrencyErrors`, `ConcurrencyResultExtensions`, mapping to `ErrorType.Conflict` | Database persistence, Dapper queries |
| **Dapper Adapter** | `ExecuteOptimisticAsync`, `OptimisticUpdateBuilder`, SQL clause formatting | Direct domain invariant modifications, database-specific driver types |
| **PostgreSQL Adapter** | `PostgreSqlConcurrencyErrorClassifier`, `XminConcurrencyToken`, `PostgreSqlLockExtensions` | Dapper dependency, Entity Framework references, cross-dialect drivers |
| **SQL Server Adapter** | `SqlServerErrorClassifier`, `SqlServerRowVersionToken`, `SqlServerLockExtensions` | Dapper dependency, PostgreSQL types, cross-dialect drivers |
| **MySQL & MariaDB Adapters** | `MySqlConcurrencyErrorClassifier`, `MariaDbConcurrencyErrorClassifier`, locking extensions | Cross-dialect driver dependencies |
| **Oracle Adapter** | `OracleConcurrencyErrorClassifier`, `OracleRowScnToken`, locking extensions | Cross-dialect driver dependencies |
| **SQLite Adapter** | `SqliteConcurrencyErrorClassifier` | Server database driver dependencies |
| **Mediator Adapter** | `IConcurrencyAwareRequest`, `ConcurrencyBehavior`, OpenTelemetry activity tracing | Direct database access, business rules |

---

## 2. Zero-Allocation & Native AOT Guarantees

1. **Readonly Record Structs**: `ConcurrencyVersion`, `ConcurrencyVersion<TEntity>`, `ConcurrencyToken`, `ExpectedVersion`, `ActualVersion`, and `XminConcurrencyToken` are value types that incur zero heap allocation during comparisons, equality checks, and method passing.
2. **Trim Analyzers & AOT Safety**:
   - `EnableTrimAnalyzer=true` and `TreatWarningsAsErrors=true` enforced across all source assemblies.
   - `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]` applied to DI resolver registrations.
   - Zero reflection-based member resolution at runtime.
3. **Allocation-Light Mediator Behaviors**: `ConcurrencyBehavior<TRequest, TResponse>` is a `sealed class` (not a struct) that operates directly on generic struct continuations (`INext<TResponse>`), eliminating async state machine allocations when no conflict occurs. The zero-allocation property of the pipeline is provided by the `INext<TResponse>` struct delegates from `EricksonLopez.Mediator`, not by the behavior class itself.

---

## 3. Immutability & Thread Safety

- All tokens, versions, and conflict models are deeply immutable.
- Checkers and conflict classifiers are thread-safe and stateless singleton instances (`OptimisticConcurrencyChecker.Instance`, `PostgreSqlConcurrencyErrorClassifier`).
- In-memory CAS transitions in `ConcurrencyController` perform atomic lock-synchronized evaluations on entity instances without exposing mutable state.
