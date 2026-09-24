# Best Practices & Anti-Patterns Guide

Design directives, engineering principles, and anti-patterns for optimistic concurrency control with **`EricksonLopez.Concurrency`**.

---

## 🏆 Engineering Best Practices

### 1. Allocation & Performance Invariants
- **Use Value-Type Primitives (`readonly record struct`)**: Always use `ConcurrencyVersion`, `ExpectedVersion`, and `ConcurrencyToken`. Avoid wrapping them in reference-type wrappers or performing unnecessary boxing to `object`.
- **Leverage `ISpanParsable` and `ISpanFormattable`**: In high-throughput HTTP endpoints, process incoming headers (`If-Match`) by passing `ReadOnlySpan<char>` directly to `ConcurrencyVersion.TryParse`.
- **Pool In-Memory Resources**: Rely on `IConcurrencyController`'s internal striped semaphore pooling rather than creating ad-hoc synchronization objects per entity.

### 2. Zero-Roundtrip Atomic Database Writes
- **Avoid Pre-Read `SELECT` for Version Checks**: The legacy pattern of reading the row before updating introduces a Time-Of-Check to Time-Of-Use (TOCTOU) race condition and unnecessary network latency.
- **Enforce Verification at the Storage Engine Boundary**: Execute `UPDATE ... WHERE id = @Id AND version = @ExpectedVersion` and verify that `rowsAffected == 1` via `connection.ExecuteOptimisticAsync`.
- **Pass Tenant Columns in Multi-Tenant Environments**: Always supply `tenantColumn` and `tenantParam` when invoking `OptimisticUpdateBuilder.BuildVersionedUpdate` to guarantee row isolation.

### 3. Domain-Aware Conflict Reconciliation
- **Strict Rejection as Default**: Default to `ConflictResolutionStrategy.Reject`. Only allow automatic domain reconciliation (`MergeDomainSpecific`) for models with formally commutative or additive operations (such as inventory increments or account ledger deposits).
- **Audit Last-Write-Wins (LWW) Explicitly**: If `LastWriteWinsConflictResolver` is enabled, record the principal who forced the overwrite, the original version, and the technical justification in audit logs.
- **Configure In-Memory CAS Timeouts**: Set appropriate `DefaultMaxAcquisitionTimeout` and `DefaultMaxExecutionTimeout` on `ConcurrencyOptions` to prevent threadpool starvation under heavy in-memory contention.

### 4. Separation of Concerns (ADR-001)
- **Separation Between Detection and Resilience**: The library strictly detects and classifies conflicts as `Transient`, `StaleState`, or `NonRetryable`. Retry logic with exponential backoff and jitter must reside in the resilience layer (`EricksonLopez.Resilience`) or the application use case orchestrator.

---

## 🚫 Common Anti-Patterns and Mitigations

| Anti-Pattern | Negative Impact | Recommended Solution |
|---|---|---|
| **TOCTOU Pre-Check** (`SELECT` then `UPDATE`) | Race window where competing threads modify the record between read and write. | Use `connection.ExecuteOptimisticAsync` with `WHERE version = @ExpectedVersion` conditional clause. |
| **Indiscriminate Distributed Locks** (Redis Redlock on every write) | Severe network latency bottlenecks, clock drift vulnerabilities, and single point of failure. | Delegate atomic conditional verification directly to the database storage engine via OCC. |
| **Blind Overwrites (Hidden LWW)** | Concurrent client updates are silently discarded without diagnostic tracking. | Classify as a concurrency conflict and return HTTP 409 Conflict with RFC 7807 problem details. |
| **Token Rotation Omission** | Clients reuse stale tokens, leading to false conflicts or inconsistent mutations. | Issue a fresh token via `ConcurrencyToken.NewGuid()` on every persisted state mutation. |
| **Immediate Retries Without Jitter** (e.g. on SQLite `SQLITE_BUSY`) | Thundering herd retry storm exacerbating database file locks. | Apply randomized exponential jitter and enable WAL mode (`PRAGMA journal_mode = WAL;`). |
| **Reentrant In-Memory CAS Calls** | Deadlock when calling `ExecuteCasAsync` recursively on the same entity identifier. | Separate workflow steps; `IConcurrencyController` strictly throws `InvalidOperationException` on reentrant invocations. |
| **Ignoring Entity Rollback on Delegate Exception** | Partial in-memory entity mutation if an unhandled exception occurs inside `mutate`. | Use C# immutable records with `with` expressions or deep-clone mutable entities prior to CAS execution. |
