# Transactions & Isolation Boundaries

## 1. Concurrency vs Transactions

`EricksonLopez.Concurrency` and `EricksonLopez.Transactions` address orthogonal concerns:

```text
Concurrency
    → Verifies state correctness and prevents lost updates across independent operations.

Transactions
    → Guarantees atomicity and all-or-nothing persistence across multiple write statements.
```

---

## 2. PostgreSQL Isolation Levels & Concurrency

| Isolation Level | Concurrency Behavior | Potential Anomalies |
|---|---|---|
| **READ COMMITTED** | Row-level locks on `UPDATE`. Detects version mismatch via `rows affected = 0`. | Non-repeatable reads possible if multiple reads occur in 1 transaction. |
| **REPEATABLE READ** | Generates snapshot. Conflicts raise `40001 serialization_failure`. | Classifiable as `Transient` conflict via `PostgreSqlConcurrencyErrorClassifier`. |
| **SERIALIZABLE** | Full serializability simulation. Conflicts raise `40001` or `40P01`. | Classifiable as `Transient` conflict for safe retry. |
