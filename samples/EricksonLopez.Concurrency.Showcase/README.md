# EricksonLopez.Concurrency — Official Showcase

> **Executable documentation · Learning guide · Cookbook · API reference**

This project is the **official reference implementation** of the `EricksonLopez.Concurrency` library.  
Every public API, integration pattern, and architectural decision is demonstrated here through executable, self-verifying code.

---

## Quick Start

```powershell
# Run the full showcase suite (all 12 levels, non-interactive)
dotnet run --project samples/EricksonLopez.Concurrency.Showcase/ -c Release

# Interactive menu (choose individual levels)
dotnet run --project samples/EricksonLopez.Concurrency.Showcase/ -c Release -- --menu
```

Expected terminal output ends with:
```
===============================================================================
 ALL SHOWCASE LEVELS EXECUTED SUCCESSFULLY.
===============================================================================
```

---

## Learning Path

The Showcase is organized as a progressive learning path. Each level builds on the previous one.

| Level | Class | Area | Complexity | Key APIs |
|---|---|---|---|---|
| **L00** | `Level00_Conceptual` | Conceptual Foundations | Introductory | Narrative only |
| **L01** | `Level01_QuickStart` | DI & First Verification | Beginner | `AddEricksonLopezConcurrency()`, `IConcurrencyController.VerifyVersion()` |
| **L02** | `Level02_FullConfiguration` | Full Configuration | Intermediate | `ConcurrencyOptions`, all dialect DI extensions |
| **L03** | `Level03_RealWorldUseCases` | Domain Versioning & ETags | Intermediate | `ExpectedVersion`, `ConcurrencyToken`, `IConcurrencyChecker` |
| **L04** | `Level04_AdvancedIntegration` | Dapper & Result Monad | Intermediate | `ExecuteOptimisticAsync()`, `CasResult.ToResult()`, `ConcurrencyErrors` |
| **L05** | `Level05_ProcessingAndConcurrency` | Atomic CAS & Race Conditions | Intermediate | `IConcurrencyController.ExecuteCasAsync()` |
| **L06** | `Level06_ErrorHandlingAndClassification` | DB Error Classification | Intermediate | All `*ConcurrencyErrorClassifier` classes, exception hierarchy |
| **L07** | `Level07_ScalabilityAndThroughput` | Zero-Alloc & OpenTelemetry | Advanced | `ConcurrencyDiagnostics`, `OptimisticConcurrencyChecker` |
| **L08** | `Level08_CustomizationAndExtensibility` | All Conflict Resolvers | Advanced | `DelegateConflictResolver`, `RefreshAndRetryConflictResolver`, `LastWriteWinsConflictResolver` |
| **L09** | `Level09_SpecializedTokensAndLocking` | Engine Tokens & Pessimistic Hints | Advanced | `XminConcurrencyToken`, `SqlServerRowVersionToken`, `OracleRowScnToken`, locking extensions |
| **L10** | `Level10_EnterpriseArchitecture` | CQRS, Mediator, Multi-Tenancy | Expert | `ConcurrencyBehavior<T,R>`, `ConcurrencyProblemDetails`, `FakeConcurrencyController` |
| **L11** | `Level11_ComprehensiveApiCoverageDemo` | Full API Surface Verification | Expert | All packages · Metadata pipeline end-to-end |

---

## Package Dependency Map

```
EricksonLopez.Concurrency.Abstractions   ← Core contracts (interfaces, value types, enums)
         ↑
EricksonLopez.Concurrency                ← Core engine (DI, resolvers, diagnostics, CAS controller)
         ↑
┌────────────────────────────────────────────────────────────────────┐
│  EricksonLopez.Concurrency.Result      ← Result monad bridge       │
│  EricksonLopez.Concurrency.Dapper      ← Dapper optimistic updates │
│  EricksonLopez.Concurrency.AspNetCore  ← HTTP middleware & ETags   │
│  EricksonLopez.Concurrency.Mediator    ← Mediator pipeline behavior│
│  EricksonLopez.Concurrency.Testing     ← Test doubles & builders   │
│  EricksonLopez.Concurrency.PostgreSql  ← PG classifier & xmin     │
│  EricksonLopez.Concurrency.SqlServer   ← SS classifier & RowVersion│
│  EricksonLopez.Concurrency.MySql       ← MySQL classifier          │
│  EricksonLopez.Concurrency.MariaDb     ← MariaDB classifier        │
│  EricksonLopez.Concurrency.Oracle      ← Oracle classifier & SCN   │
│  EricksonLopez.Concurrency.Sqlite      ← SQLite classifier         │
└────────────────────────────────────────────────────────────────────┘
```

---

## Domain Models

The Showcase uses three purpose-built domain models that represent the three entity interface contracts:

| Model | Interface | Scenario |
|---|---|---|
| `BankAccount` | `IVersionedEntity<BankAccount>`, `IMutableVersionedEntity` | Numeric optimistic versioning |
| `CustomerProfile` | `IConcurrencyAware` | Opaque ETag / concurrency token |
| `ProductInventory` | `IVersionedEntity<ProductInventory>`, `IMutableVersionedEntity` | Inventory stock reservation |

---

## Architecture Invariants (ADR-001)

> **This library detects and classifies concurrency conflicts.  
> It does NOT retry, backoff, or circuit-break.  
> Those concerns belong to Resilience (Polly) or application orchestration.**

- `Transient` / `Retryable` classifications → signal to the caller that a retry is safe  
- `NonRetryable` / `Fatal` classifications → signal that human or domain intervention is required  
- `ThrowOnUnresolvedConflict = true` → controller throws `ConcurrencyException` instead of returning a conflict model

---

## Key API Reference

### Conflict Resolution Strategies

| Resolver | Strategy | Use When |
|---|---|---|
| `RejectConflictResolver<T>.Instance` | `Reject` | Strict: surface conflict to caller (default) |
| `LastWriteWinsConflictResolver<T>.Instance` | `LastWriteWinsExplicit` | Explicit opt-in overwrite (use with caution) |
| `DelegateConflictResolver<T>` | `MergeDomainSpecific` | Domain-specific merge logic inline |
| `RefreshAndRetryConflictResolver<T>` | `RefreshAndRetry` | Reload from storage and re-apply mutations |

### Conflict Classification → Retryability

| Classification | Retryability | Typical Cause |
|---|---|---|
| `Transient` | Transient | DB deadlock, serialization failure |
| `Retryable` | Transient | Version mismatch (safe to reload + retry) |
| `NonRetryable` | Permanent | Token mismatch (needs client re-read) |
| `StaleState` | Permanent | Entity moved far ahead |
| `Fatal` | Permanent | Corruption or configuration error |

---

## Execution Commands Reference

```powershell
# Full suite (CI-friendly)
dotnet run --project samples/EricksonLopez.Concurrency.Showcase/ -c Release

# Interactive menu
dotnet run --project samples/EricksonLopez.Concurrency.Showcase/ -c Release -- --menu

# Specific level (non-interactive mode selects level number at prompt)
dotnet run --project samples/EricksonLopez.Concurrency.Showcase/ -c Release -- --menu
# then type: 5 [Enter]

# Build only
dotnet build samples/EricksonLopez.Concurrency.Showcase/ -c Release
```

---

*Copyright © Erickson Lopez. MIT License.*
