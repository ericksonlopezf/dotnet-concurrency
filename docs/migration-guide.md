# Migration Guide & Adoption Path

## 1. Migrating from Raw SQL Updates to EricksonLopez.Concurrency

### Step 1: Update Domain Entities
Add `IVersionedEntity` or `IConcurrencyAware` to your domain aggregate roots:

```csharp
// Before:
public class Order
{
    public string Id { get; set; }
    public decimal Total { get; set; }
}

// After:
public class Order : IVersionedEntity
{
    public string Id { get; init; } = string.Empty;
    public decimal Total { get; set; }
    public long Version { get; set; }
}
```

---

### Step 2: Update Repository Update Statements
Replace manual non-versioned updates with `OptimisticUpdateBuilder` and `ExecuteOptimisticAsync`:

```csharp
// Before:
await connection.ExecuteAsync("UPDATE orders SET total = @Total WHERE id = @Id", order);

// After:
string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
    tableName: "orders",
    setClauses: "total = @Total",
    idColumn: "id",
    versionColumn: "version");

ConcurrencyConflict? conflict = await connection.ExecuteOptimisticAsync(
    sql: sql,
    param: new { order.Id, order.Total, ExpectedVersion = (long)expectedVersion.Version },
    expectedVersion: expectedVersion,
    entityId: order.Id,
    entityType: nameof(Order));

if (conflict is not null)
{
    return Result.Failure(ConcurrencyErrors.FromConflict(conflict));
}
```

---

### Step 3: Register Pipeline Behaviors
Add concurrency services and mediator behavior in `Program.cs`:

```csharp
builder.Services.AddEricksonLopezConcurrency();
builder.Services.AddConcurrencyMediatorBehavior();
```
