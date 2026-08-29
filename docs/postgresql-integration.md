# PostgreSQL Integration & SQLSTATE Classification

## 1. PostgreSQL Concurrency Error Classifier

`PostgreSqlConcurrencyErrorClassifier` classifies PostgreSQL-specific exceptions (`Npgsql.PostgresException`) into structured `ConcurrencyConflict` records based on standard PostgreSQL SQLSTATE error codes:

| SQLSTATE Code | Name | Classification | Meaning |
|---|---|---|---|
| `40001` | `serialization_failure` | `Transient` (Retryable) | Concurrent transaction committed conflicting write under `SERIALIZABLE` or `REPEATABLE READ`. |
| `40P01` | `deadlock_detected` | `Transient` (Retryable) | PostgreSQL killed one transaction to resolve mutual dependency cycle. |
| `55P03` | `lock_not_available` | `NonRetryable` | `NOWAIT` lock failed to obtain row lock immediately. |
| `23505` | `unique_violation` | `StaleState` | Concurrent insert created a duplicate key or token. |

```csharp
try
{
    await repository.SaveAsync(entity, ct);
}
catch (PostgresException pgEx) when (PostgreSqlConcurrencyErrorClassifier.IsTransient(pgEx))
{
    ConcurrencyConflict? conflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(pgEx, entity.Id, nameof(MyEntity));
    return Result.Failure(ConcurrencyErrors.FromConflict(conflict!));
}
```

---

## 2. PostgreSQL `xmin` System Column Integration

PostgreSQL maintains an internal `xmin` system column containing the 32-bit transaction ID (XID) that inserted or updated each row. 

`XminConcurrencyToken` maps this column directly into an `IConcurrencyToken` without requiring additional table schema migration:

```sql
SELECT id, name, price, xmin FROM products WHERE id = @Id;
```

```csharp
// Read xmin directly as uint
uint xmin = reader.GetFieldValue<uint>(reader.GetOrdinal("xmin"));
var token = new XminConcurrencyToken(xmin);

// Compare against client token
bool isValid = token.Equals(expectedToken);
```

---

## 3. Explicit Row-Level Locking Helper (`FOR UPDATE`)

When pessimistic synchronization is strictly required by the domain within a transactional boundary:

```csharp
string sql = "SELECT * FROM accounts WHERE id = @Id"
    .WithLock(PostgreSqlLockMode.ForUpdateNoWait);

// Result:
// SELECT * FROM accounts WHERE id = @Id FOR UPDATE NOWAIT;
```
