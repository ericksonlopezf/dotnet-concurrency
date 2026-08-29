# Conflict Detection & Classification

## 1. The ConcurrencyConflict Model

When a concurrency discrepancy occurs, `EricksonLopez.Concurrency` creates a structured, immutable `ConcurrencyConflict` record rather than throwing raw exceptions:

```csharp
public sealed record ConcurrencyConflict
{
    public string EntityId { get; init; }
    public string EntityType { get; init; }
    public ConcurrencyConflictType ConflictType { get; init; }
    public ConcurrencyConflictClassification Classification { get; init; }
    public ExpectedVersion? ExpectedVersion { get; init; }
    public ActualVersion? ActualVersion { get; init; }
    public IConcurrencyToken? ExpectedToken { get; init; }
    public IConcurrencyToken? ActualToken { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? Operation { get; init; }
    public string Message { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}
```

---

## 2. Conflict Types & Classifications

### ConcurrencyConflictType

| Value | Description |
|---|---|
| `VersionMismatch` | The actual database or entity version differs from the caller's `ExpectedVersion`. |
| `TokenMismatch` | The actual opaque token (e.g. ETag) does not match the caller's `ExpectedToken`. |
| `StateDeleted` | The target entity has been deleted by another concurrent process. |
| `AlreadyExists` | The entity already exists when `ExpectedVersion.New` was asserted. |
| `SerializationFailure` | Database failed to serialize the transaction (e.g., SQLSTATE `40001` or snapshot conflict `3960`). |
| `Deadlock` | Database engine detected a deadlock graph (e.g., SQLSTATE `40P01`, SQL Server `1205`, MySQL `1213`). |
| `LockUnavailable` | Requested lock could not be obtained immediately under `NOWAIT` / lock timeout (e.g., SQLSTATE `55P03`, `1222`). |
| `Custom` | Domain-specific concurrency arbitration violation. |

### ConcurrencyConflictClassification

| Classification | Meaning | Recommended Action |
|---|---|---|
| `Transient` | Caused by brief lock contention or serialization anomaly (e.g., deadlock, 40001). | Safe for automated exponential backoff retry via `EricksonLopez.Resilience`. |
| `StaleState` | The client based their update on an outdated version of the entity. | Re-fetch fresh state, re-evaluate business invariants, or prompt the user. |
| `NonRetryable` | The entity was deleted or a structural invariant prevents any retry. | Abort immediately; return permanent conflict error to caller. |
