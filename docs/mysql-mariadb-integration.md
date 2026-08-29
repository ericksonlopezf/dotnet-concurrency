# MySQL & MariaDB Concurrency Integration Guide

## Overview

The `EricksonLopez.Concurrency.MySql` and `EricksonLopez.Concurrency.MariaDb` packages provide native MySQL and MariaDB concurrency support with locking clauses and error classifications using `MySqlConnector`.

---

## 1. Locking Modes and Extensions

### MySQL Locking

```csharp
string query = "SELECT * FROM orders WHERE id = @Id;".WithMySqlLock(MySqlLockMode.ForUpdateNowait);
// Output: "SELECT * FROM orders WHERE id = @Id FOR UPDATE NOWAIT;"
```

Supported modes:
- `ForUpdate`: `FOR UPDATE`
- `ForUpdateNowait`: `FOR UPDATE NOWAIT`
- `ForUpdateSkipLocked`: `FOR UPDATE SKIP LOCKED`
- `ForShare`: `FOR SHARE`

### MariaDB Locking

```csharp
string query = "SELECT * FROM orders WHERE id = @Id;".WithMariaDbLockWait(5);
// Output: "SELECT * FROM orders WHERE id = @Id FOR UPDATE WAIT 5;"
```

Supported modes:
- `ForUpdate`: `FOR UPDATE`
- `ForUpdateNowait`: `FOR UPDATE NOWAIT`
- `ForUpdateSkipLocked`: `FOR UPDATE SKIP LOCKED`
- `LockInShareMode`: `LOCK IN SHARE MODE`
- `WithMariaDbLockWait(seconds)`: `FOR UPDATE WAIT n`

---

## 2. Error Classification

Both MySQL and MariaDB classifiers handle common storage engine error codes:

| Error Code | Condition | Classification |
|---|---|---|
| `1213` | Deadlock found when trying to get lock | `Deadlock` (Transient) |
| `1205` | Lock wait timeout exceeded | `LockNotAvailable` (Non-transient) |
| `1062` | Duplicate entry for key | `UniqueViolation` (Permanent) |

---

## 3. Dependency Injection

```csharp
// For MySQL:
builder.Services.AddEricksonLopezConcurrencyMySql();

// For MariaDB:
builder.Services.AddEricksonLopezConcurrencyMariaDb();
```
