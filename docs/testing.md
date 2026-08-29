# Testing & Concurrency Verification

`EricksonLopez.Concurrency` provides first-class testing utilities and mock-free test doubles in `EricksonLopez.Concurrency.Testing` to make unit testing domain handlers effortless and reliable.

---

## 1. Test Suite Architecture

| Project | Target Scope | Key Scenarios |
|---|---|---|
| `EricksonLopez.Concurrency.Testing` | Testing framework | `FakeConcurrencyController`, `ConcurrencyConflictBuilder` |
| `UnitTests` | In-memory invariants & components | Version comparisons, token formatting, CAS transitions, resolver behaviors, result mapping |
| `ArchitectureTests` | NetArchTest rules | Layer isolation, zero database driver references in Domain/Application |
| `IntegrationTests` | High concurrency & real storage | Concurrent writers competing for 1 entity, SQLite Dapper updates, multi-tenant isolation |
| `Benchmarks` | BenchmarkDotNet harness | Allocation profiles, throughput, comparison microbenchmarks |

---

## 2. Using `FakeConcurrencyController` in Unit Tests

Instead of setting up fragile mocking frameworks for `IConcurrencyController`, use `FakeConcurrencyController`:

### 2.1 Simulating Successful State Transitions

```csharp
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Testing;
using Xunit;

public class UpdateBalanceHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_WhenNoConflictOccurs()
    {
        // 1. Arrange fake controller
        var fakeController = new FakeConcurrencyController();
        fakeController.WithSuccess(nextVersion: 2L);

        var handler = new UpdateBalanceHandler(fakeController, repository);
        var command = new UpdateBalanceCommand("acc-1", 150m, ExpectedVersion.Specific(1));

        // 2. Act
        var result = await handler.Handle(command, CancellationToken.None);

        // 3. Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, fakeController.TotalInvocations);
        Assert.Equal("acc-1", fakeController.ExecuteCasInvocations[0].EntityId);
    }
}
```

### 2.2 Simulating Concurrency Conflicts & Transient Retries

```csharp
[Fact]
public async Task Handle_ShouldReportConflict_WhenVersionMismatchOccurs()
{
    var fakeController = new FakeConcurrencyController();
    
    // Simulate a VersionMismatch conflict on the next write only
    fakeController.WithConflictOnNextWrite(
        ConcurrencyConflictType.VersionMismatch,
        entityId: "acc-1",
        entityType: "BankAccount",
        classification: ConcurrencyConflictClassification.Transient);

    var handler = new UpdateBalanceHandler(fakeController, repository);
    var command = new UpdateBalanceCommand("acc-1", 150m, ExpectedVersion.Specific(1));

    var result = await handler.Handle(command, CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("VersionMismatch", result.Error.Code);
}
```

### 2.3 Fluent Test Conflict Construction with `ConcurrencyConflictBuilder`

```csharp
var conflict = new ConcurrencyConflictBuilder()
    .WithEntityId("order-101")
    .WithEntityType("Order")
    .WithConflictType(ConcurrencyConflictType.Deadlock)
    .WithClassification(ConcurrencyConflictClassification.Transient)
    .WithVersions(ExpectedVersion.Specific(5), ActualVersion.From(6))
    .WithMetadata("sqlstate", "1205")
    .Build();

fakeController.WithConflict(conflict);
```

---

## 3. High-Concurrency Stress Testing (Integration Tests)

Integration tests utilize async coordination latches (`TaskCompletionSource`) to synchronize multiple competing tasks:

```csharp
var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

Task<CasResult<Account>>[] tasks = Enumerable.Range(0, 100).Select(i => Task.Run(async () =>
{
    await startSignal.Task; // All writers released simultaneously
    return await controller.ExecuteCasAsync(
        entity,
        ExpectedVersion.Specific(1),
        "acc-1",
        (acc, ct) => { acc.Balance += 10; return ValueTask.FromResult(acc); });
})).ToArray();

startSignal.SetResult();
CasResult<Account>[] results = await Task.WhenAll(tasks);

// Exactly 1 writer should succeed; 99 should receive VersionMismatch conflicts
Assert.Equal(1, results.Count(r => r.IsSuccess));
Assert.Equal(99, results.Count(r => r.IsConflict));
```
