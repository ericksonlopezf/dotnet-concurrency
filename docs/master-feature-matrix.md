# Master Feature & Dialect Compatibility Matrix

This document provides a comprehensive technical matrix detailing concurrency controls, database dialect translation mechanisms, error classification, and runtime guarantees across the `EricksonLopez.Concurrency` ecosystem.

---

## 1. Concurrency Control Mechanisms Matrix

| Capability | In-Memory CAS (`ExecuteCasAsync`) | Version Token (`ConcurrencyVersion`) | ETag / Token (`ConcurrencyToken`) | PostgreSQL `xmin` | SQL Server `rowversion` | Database Dialect Native |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Allocation Profile** | Zero-Heap (Struct) | Zero-Heap (Struct) | Low (String Interned) | Zero-Heap (Struct) | Zero-Heap (Struct) | Driver-level |
| **Native AOT Compatible** | Yes (Trim-safe) | Yes (Trim-safe) | Yes (Trim-safe) | Yes (Trim-safe) | Yes (Trim-safe) | Yes (Trim-safe) |
| **Monotonic Versioning** | Yes (`Version.Next()`) | Yes (`Value + 1`) | No (Opaque token) | System-managed | System-managed | Database sequence |
| **`ISpanParsable<T>`** | N/A | Yes | Yes | Yes | Yes | N/A |
| **HTTP ETag Mapping** | N/A | Yes | Yes | Yes | Yes | N/A |
| **Automatic Classification**| Yes | Yes | Yes | Yes | Yes | Yes (SQLSTATE / Error codes) |

---

## 2. Database Dialect Integration Matrix

| Database Engine | Integration Package | Error Classifier | Native Concurrency Token | Row Lock Syntax Supported | Deadlock Code / SQLSTATE |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **PostgreSQL** | `EricksonLopez.Concurrency.PostgreSql` | `PostgreSqlConcurrencyErrorClassifier` | `XminConcurrencyToken` (uint32) | `FOR UPDATE [NOWAIT / SKIP LOCKED]` | `40001` (Serialization), `40P01` (Deadlock), `55P03` (LockNotAvailable) |
| **SQL Server** | `EricksonLopez.Concurrency.SqlServer` | `SqlServerErrorClassifier` | `SqlServerRowVersionToken` (byte[8]) | `WITH (UPDLOCK, ROWLOCK)` | `1205` (Deadlock), `1222` (Lock Request Timeout) |
| **MySQL** | `EricksonLopez.Concurrency.MySql` | `MySqlConcurrencyErrorClassifier` | Version Column (`BIGINT UNSIGNED`) | `FOR UPDATE [NOWAIT / SKIP LOCKED]` | `1213` (Deadlock), `1205` (Lock Wait Timeout) |
| **MariaDB** | `EricksonLopez.Concurrency.MariaDb` | `MariaDbConcurrencyErrorClassifier` | Version Column (`BIGINT UNSIGNED`) | `FOR UPDATE [WAIT n / NOWAIT]` | `1213` (Deadlock), `1205` (Lock Wait Timeout) |
| **Oracle** | `EricksonLopez.Concurrency.Oracle` | `OracleConcurrencyErrorClassifier` | `ORA_ROWSCN` / Number | `FOR UPDATE [NOWAIT / WAIT n]` | `ORA-00060` (Deadlock), `ORA-00054` (Resource Busy) |
| **SQLite** | `EricksonLopez.Concurrency.Sqlite` | `SqliteConcurrencyErrorClassifier` | Version Column (`INTEGER`) | Table lock semantics | `SQLITE_BUSY` (5), `SQLITE_LOCKED` (6) |

---

## 3. Framework & Ecosystem Interoperability

| Framework / Layer | Integration Package | Mechanism | Error Model |
| :--- | :--- | :--- | :--- |
| **ASP.NET Core** | `EricksonLopez.Concurrency.AspNetCore` | `ConcurrencyConflictMiddleware`, `Results.Extensions` | RFC 7807 `ConcurrencyProblemDetails` (HTTP 409 Conflict) |
| **EricksonLopez.Result** | `EricksonLopez.Concurrency.Result` | `.ToResult()`, `CasResult<T>.ToResult()` | `Error.Conflict("Concurrency.Conflict", ...)` |
| **EricksonLopez.Mediator** | `EricksonLopez.Concurrency.Mediator` | `ConcurrencyBehavior<TRequest, TResponse>` | OpenTelemetry span tagging & metrics recording |
| **Dapper** | `EricksonLopez.Concurrency.Dapper` | `ConcurrencyVersionTypeHandler`, `ConcurrencyTokenTypeHandler` | Parameter mapping & column deserialization |
| **Testing** | `EricksonLopez.Concurrency.Testing` | `FakeConcurrencyController`, `ConcurrencyConflictBuilder` | In-memory verification without test doubles |

---

## 4. Conflict Classification & Resolution Matrix

| Conflict Classification | Recoverability | Default Strategy | Trigger Conditions | Recommended Action |
| :--- | :--- | :--- | :--- | :--- |
| **`Transient`** | Highly Recoverable | Auto-Retry with Jitter | Database lock timeout, temporary serialization race | Re-execute with `EricksonLopez.Resilience` retry policy |
| **`StaleState`** | Conditionally Recoverable | Refresh & Domain Merge | Version mismatch (`Expected != Actual`), ETag divergence | Use `RefreshAndRetryConflictResolver<T>` to re-apply changes |
| **`NonRetryable`** | Non-Recoverable | Immediate Rejection | Entity modified beyond automatic reconciliation rules | Return HTTP 409 Conflict to client with latest state |
| **`Fatal`** | Non-Recoverable | Abort & Log Alert | Entity permanently deleted, schema version corruption | Return HTTP 410 Gone / 500 Internal Error |
