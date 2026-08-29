# Oracle Concurrency Integration Guide

## Overview

The `EricksonLopez.Concurrency.Oracle` package provides native Oracle Database support for optimistic concurrency control, System Change Number (`ORA_ROWSCN`) tokens, and ORA error classification.

---

## 1. Concurrency Tokens: `ORA_ROWSCN`

Oracle Database allows querying the System Change Number of a row via pseudo-column `ORA_ROWSCN` (enabled with `ROWDEPENDENCIES` at table creation).

### Schema Example

```sql
CREATE TABLE orders (
    id VARCHAR2(50) PRIMARY KEY,
    status VARCHAR2(20),
    amount NUMBER(18, 2)
) ROWDEPENDENCIES;
```

### C# Usage

```csharp
var token = new OracleRowScnToken(1024567890L);
Console.WriteLine(token.Value); // "1024567890"
```

---

## 2. Locking Extensions

```csharp
string sql = "SELECT * FROM orders WHERE id = @Id;".WithOracleLock(OracleLockMode.ForUpdateNowait);
// Output: "SELECT * FROM orders WHERE id = @Id FOR UPDATE NOWAIT;"

string sqlWait = "SELECT * FROM orders WHERE id = @Id;".WithOracleLockWait(10);
// Output: "SELECT * FROM orders WHERE id = @Id FOR UPDATE WAIT 10;"
```

---

## 3. Oracle Error Classification

`OracleConcurrencyErrorClassifier` maps Oracle error codes to structured domain conflicts:

| ORA Code | Condition | Classification |
|---|---|---|
| `ORA-00060` | Deadlock detected while waiting for resource | `Deadlock` (Transient) |
| `ORA-00054` | Resource busy and acquire with NOWAIT specified | `LockNotAvailable` (Non-transient) |
| `ORA-08177` | Cannot serialize access for this transaction | `SerializationFailure` (Transient) |
| `ORA-00001` | Unique constraint violated | `UniqueViolation` (Permanent) |
| `ORA-03113`, `ORA-12170` | End-of-file on communication channel / Connect timeout | Transient Connection Drop |

---

## 4. Dependency Injection

```csharp
builder.Services.AddEricksonLopezConcurrencyOracle();
```
