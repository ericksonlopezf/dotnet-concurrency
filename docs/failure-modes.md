# Failure Modes & Threat Analysis

## 1. Concurrency Failure Matrix

| Failure Mode | Root Cause | Detection Point | Handling Strategy |
|---|---|---|---|
| **Lost Update** | Multiple writers read state concurrently; last write blindly overwrites earlier updates. | Database conditional `WHERE version = @ExpectedVersion` | Fails with 0 rows affected; classified as `ConcurrencyConflict.VersionMismatch`. |
| **Phantom Update / Stale Cache** | Client submits update based on stale cached read replica. | In-memory `OptimisticConcurrencyChecker` | Fails fast before reaching database; returns `ErrorType.Conflict`. |
| **PostgreSQL Serialization Failure** | Concurrent transactions mutate intersecting key ranges under `SERIALIZABLE`. | PostgreSQL `SQLSTATE 40001` | Classified as `Transient` conflict; candidate for automated exponential backoff retry. |
| **PostgreSQL Deadlock** | Two transactions lock rows in reverse order. | PostgreSQL `SQLSTATE 40P01` | Classified as `Transient` conflict; logged and retried via resilience layer. |
| **Cross-Tenant Key Collision** | Identical entity ID updated without tenant scoping. | `OptimisticUpdateBuilder` `tenant_id` clause | Prevented structurally; update affects 0 rows if tenant does not match. |
| **Entity Deletion Race** | Entity deleted by Process A while Process B is updating it. | Actual state not found (`ActualVersion.NotFound`) | Classified as `NonRetryable` conflict (`StateDeleted`). |

---

## 2. Security Threat Model

1. **Information Leakage via Error Messages**: `ConcurrencyErrors` sanitized metadata prevents internal database stack traces or SQL connection strings from leaking to public HTTP consumers.
2. **Timing Attacks on Token Validation**: `ConcurrencyToken.Equals` compares opaque token strings using constant-time evaluation properties where appropriate.
3. **Cross-Tenant State Modification**: Guaranteed prevented by enforcing tenant compound keys and PostgreSQL RLS compatibility.
