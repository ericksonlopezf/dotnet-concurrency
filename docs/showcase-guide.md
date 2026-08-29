# Official Showcase Guide: EricksonLopez.Concurrency

The **`EricksonLopez.Concurrency.Showcase`** project (`samples/EricksonLopez.Concurrency.Showcase`) represents the **official executable reference implementation** and interactive educational demonstration for the optimistic concurrency framework in the **EricksonLopez** .NET 10 ecosystem.

---

## 🎯 Purpose and Philosophy

1. **Executable Documentation**: Every concept, class, struct, enum, and extension method documented is backed by real, compilable, and executable C# code.
2. **Zero Fictitious APIs**: The Showcase uses strictly existing components in `src/EricksonLopez.Concurrency*` packages.
3. **Pedagogical Progression**: Structured across 11 progressive levels (Level 00 to Level 10), enabling a structured learning curve from core theoretical concepts to enterprise distributed architectures.
4. **Official Reference Standard**: When in doubt regarding how to integrate any component of the library, the Showcase represents the authorized reference implementation.

---

## 🚀 How to Run the Showcase

### Run All Levels in Batch Mode
```bash
dotnet run --project samples/EricksonLopez.Concurrency.Showcase --framework net10.0 -- --run-all
```

### Run Interactive CLI Menu
```bash
dotnet run --project samples/EricksonLopez.Concurrency.Showcase --framework net10.0 -- --menu
```

---

## 📚 Pedagogical Level Map

| Level | Title | Source File | Description |
|---|---|---|---|
| **00** | Conceptual & Design Principles | [`Level00_Conceptual.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level00_Conceptual.cs) | Motivation, Lost Updates problem, comparison against distributed locks (Redis Redlock), zero-allocation structs, and Native AOT. |
| **01** | Quick Start | [`Level01_QuickStart.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level01_QuickStart.cs) | Dependency injection setup with `AddEricksonLopezConcurrency`, `IVersionedEntity` entity, and first verification with `IConcurrencyController`. |
| **02** | Full Configuration | [`Level02_FullConfiguration.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level02_FullConfiguration.cs) | `ConcurrencyOptions` configuration, custom resolver registration (`AddConflictResolver`), and full database dialect registrations (`PostgreSQL`, `SQL Server`, `MySQL`, `MariaDB`, `Oracle`, `SQLite`, `Mediator`, `AspNetCore`). |
| **03** | Real-World Use Cases | [`Level03_RealWorldUseCases.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level03_RealWorldUseCases.cs) | Strongly-typed versions `IVersionedEntity<T>` (`ConcurrencyVersion<TEntity>`), `ExpectedVersion` semantics (`New`, `Exists`, `Specific`, `Any`), `ActualVersion`, and REST/ETag validation with `ConcurrencyToken`. |
| **04** | Advanced Integration | [`Level04_AdvancedIntegration.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level04_AdvancedIntegration.cs) | Dapper zero-roundtrip execution with `OptimisticUpdateBuilder.BuildVersionedUpdate`, `ExecuteOptimisticAsync`, token updates via `ExecuteOptimisticTokenAsync`, raw row count evaluation with `FromRowsAffected`, and monadic mapping to `EricksonLopez.Result`. |
| **05** | Processing & Concurrency | [`Level05_ProcessingAndConcurrency.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level05_ProcessingAndConcurrency.cs) | In-memory Compare-And-Swap (`ExecuteCasAsync`), atomic state transitions, and parallel race condition simulation across 10 concurrent tasks. |
| **06** | Error Handling & Classification | [`Level06_ErrorHandlingAndClassification.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level06_ErrorHandlingAndClassification.cs) | Database error classification matrix for PostgreSQL (`40001`, `40P01`, `55P03`), SQL Server (`1205`, `3960`, `1222`), MySQL/MariaDB (`1213`, `1205`), Oracle (`ORA-00060`, `ORA-08177`, `ORA-00054`), SQLite (`5`, `6`, `19`), and full `ConcurrencyException` hierarchy. |
| **07** | Scalability & Throughput | [`Level07_ScalabilityAndThroughput.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level07_ScalabilityAndThroughput.cs) | Zero-allocation verification (0 bytes heap allocated across 1,000,000 checks at 50M+ ops/sec) and OpenTelemetry distributed tracing/metrics (`ConcurrencyDiagnostics`). |
| **08** | Customization & Extensibility | [`Level08_CustomizationAndExtensibility.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level08_CustomizationAndExtensibility.cs) | Implementing `IConcurrencyConflictResolver<T>`, domain-specific reconciliation with `ConflictResolution.Merged`, `DelegateConflictResolver`, explicit Last-Write-Wins (`LastWriteWinsConflictResolver`), and reloading state with `RefreshAndRetryConflictResolver`. |
| **09** | Specialized Tokens & Locking | [`Level09_SpecializedTokensAndLocking.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level09_SpecializedTokensAndLocking.cs) | Native database tokens `XminConcurrencyToken` (PostgreSQL), `SqlServerRowVersionToken` (SQL Server), `OracleRowScnToken` (Oracle), and pessimistic query locking helpers (`FOR UPDATE`, `WITH (UPDLOCK)`, `WAIT`). |
| **10** | Enterprise Architecture | [`Level10_EnterpriseArchitecture.cs`](../samples/EricksonLopez.Concurrency.Showcase/Levels/Level10_EnterpriseArchitecture.cs) | Clean Architecture + CQRS with `EricksonLopez.Mediator`, `ConcurrencyBehavior`, multi-tenancy SQL isolation, test double harness (`FakeConcurrencyController`, `ConcurrencyConflictBuilder`), ASP.NET Core RFC 7807 `ConcurrencyProblemDetails`, and ADR-001 boundary separation. |

---

## 🏛️ Ecosystem Structure

```
├── src/
│   ├── EricksonLopez.Concurrency.Abstractions/   # Core domain contracts, zero-allocation structs, and token abstractions
│   ├── EricksonLopez.Concurrency/                # In-memory controller, checker, and OpenTelemetry
│   ├── EricksonLopez.Concurrency.Result/         # Monadic Result and Error conversion extensions
│   ├── EricksonLopez.Concurrency.Dapper/         # SQL query builder and ExecuteOptimistic extensions
│   ├── EricksonLopez.Concurrency.PostgreSql/     # xmin token and SQLSTATE error classifier
│   ├── EricksonLopez.Concurrency.SqlServer/      # ROWVERSION token and SqlException classifier
│   ├── EricksonLopez.Concurrency.MySql/          # MySqlException classifier and lock mode helpers
│   ├── EricksonLopez.Concurrency.MariaDb/        # MariaDB classifier and timed lock mode helpers
│   ├── EricksonLopez.Concurrency.Oracle/         # ORA_ROWSCN token and ORA error classifier
│   ├── EricksonLopez.Concurrency.Sqlite/         # SQLITE_BUSY / SQLITE_LOCKED error classifier
│   ├── EricksonLopez.Concurrency.Mediator/       # Pipeline behavior for IConcurrencyAwareRequest
│   ├── EricksonLopez.Concurrency.Testing/        # Test double FakeConcurrencyController and ConcurrencyConflictBuilder
│   └── EricksonLopez.Concurrency.AspNetCore/    # RFC 7807 ProblemDetails, ETag headers, and middleware
├── samples/
│   └── EricksonLopez.Concurrency.Showcase/       # Official executable reference showcase
├── tests/                                        # 16 unit, integration, architecture, and AOT smoke test suites
└── docs/                                         # Comprehensive technical documentation & cookbook
```
