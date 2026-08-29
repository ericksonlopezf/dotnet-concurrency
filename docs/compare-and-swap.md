# Compare-And-Swap (CAS)

## 1. Overview & Mechanics

Compare-And-Swap (CAS) is an atomic synchronization primitive where a state change is performed **only if** the current state matches the expected version. If the condition holds, the state is updated and the version is automatically incremented.

`IConcurrencyController` provides an in-memory CAS execution mechanism:

```csharp
public ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
    TEntity entity,
    ExpectedVersion expectedVersion,
    string entityId,
    Func<TEntity, CancellationToken, ValueTask<TEntity>> mutateAction,
    CancellationToken cancellationToken = default)
    where TEntity : class, IVersionedEntity;
```

```mermaid
flowchart TD
    Start([ExecuteCasAsync]) --> Check{Entity.Version == ExpectedVersion?}
    Check -- No --> Conflict[Return CasResult.Conflicted with VersionMismatch]
    Check -- Yes --> Mutate[Execute mutateAction]
    Mutate --> Increment[Set entity.Version = entity.Version + 1]
    Increment --> Activity[Record OpenTelemetry Success & Latency]
    Activity --> Done[Return CasResult.Succeeded]
```

---

## 2. Usage Example

```csharp
public async Task<Result<BankAccount>> DepositMoneyAsync(
    BankAccount account,
    decimal amount,
    ExpectedVersion expectedVersion,
    CancellationToken ct)
{
    CasResult<BankAccount> casResult = await _concurrencyController.ExecuteCasAsync(
        account,
        expectedVersion,
        account.Id,
        (acc, cancellation) =>
        {
            acc.Balance += amount;
            return ValueTask.FromResult(acc);
        },
        ct);

    return casResult.ToResult();
}
```

---

## 3. Guarantees & Invariants

1. **Atomicity**: The verification, mutation, and version incrementation are executed atomically on the entity instance.
2. **Telemetry Integration**: Automatically creates an OpenTelemetry Activity `concurrency.cas.execute` and increments metric counters.
3. **Monadic Conversion**: Directly convert any `CasResult<T>` into an `EricksonLopez.Result<T>` via `.ToResult()`.
