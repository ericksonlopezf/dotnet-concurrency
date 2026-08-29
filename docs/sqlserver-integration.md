# SQL Server Concurrency Integration Guide

## Overview

The `EricksonLopez.Concurrency.SqlServer` package provides native Microsoft SQL Server support for optimistic concurrency control, table hints, and transient error classification.

---

## 1. Concurrency Tokens: `ROWVERSION` / `TIMESTAMP`

SQL Server provides an automatically incrementing 8-byte binary column type (`ROWVERSION` or legacy `TIMESTAMP`). `SqlServerRowVersionToken` wraps this binary sequence as a zero-allocation `readonly struct`.

### Schema Example

```sql
CREATE TABLE dbo.Customers (
    Id NVARCHAR(50) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Balance DECIMAL(18, 2) NOT NULL,
    RowVersion ROWVERSION NOT NULL
);
```

### C# Usage

```csharp
var token = new SqlServerRowVersionToken(rowVersionBytes);
Console.WriteLine(token.Value); // e.g. "00000000000007D1"
```

---

## 2. Table Hints and Locking Extensions

The `SqlServerLockExtensions` class provides fluent helpers to append SQL Server-specific table hints:

```csharp
string query = "dbo.Customers".WithSqlServerTableHint(SqlServerLockMode.UpdLockRowLock);
// Output: "dbo.Customers WITH (UPDLOCK, ROWLOCK)"
```

Available hints:
- `UpdLockRowLock`: `WITH (UPDLOCK, ROWLOCK)`
- `UpdLockRowLockNowait`: `WITH (UPDLOCK, ROWLOCK, NOWAIT)`
- `UpdLockRowLockReadPast`: `WITH (UPDLOCK, ROWLOCK, READPAST)`
- `XLockRowLock`: `WITH (XLOCK, ROWLOCK)`
- `XLockRowLockNowait`: `WITH (XLOCK, ROWLOCK, NOWAIT)`

---

## 3. SQL Server Error Classification

`SqlServerErrorClassifier` intercepts `SqlException` instances and classifies concurrency conflicts:

| Error Number | Condition | Classification |
|---|---|---|
| `1205` | Transaction deadlock | `Deadlock` (Transient) |
| `3960`, `3961` | Snapshot isolation update conflict | `SerializationFailure` (Transient) |
| `1222` | Lock request time-out period exceeded | `LockNotAvailable` (Non-transient) |
| `2601`, `2627` | Cannot insert duplicate key / unique constraint | `UniqueViolation` (Permanent) |

---

## 4. Dependency Injection

```csharp
builder.Services.AddEricksonLopezConcurrencySqlServer();
```
