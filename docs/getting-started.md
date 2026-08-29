# Getting Started with EricksonLopez.Concurrency

## 1. Introduction

`EricksonLopez.Concurrency` provides optimistic concurrency control, deterministic versioning, and zero-roundtrip conflict detection for .NET 10 enterprise applications.

---

## 2. Installation

Add the required NuGet packages to your project:

```bash
dotnet add package EricksonLopez.Concurrency
dotnet add package EricksonLopez.Concurrency.Result
dotnet add package EricksonLopez.Concurrency.Dapper
dotnet add package EricksonLopez.Concurrency.PostgreSql
dotnet add package EricksonLopez.Concurrency.Mediator
```

---

## 3. Quick Setup in ASP.NET Core

```csharp
// Program.cs
builder.Services.AddEricksonLopezConcurrency(options =>
{
    options.DefaultResolutionStrategy = ConflictResolutionStrategy.Reject;
    options.EnableDiagnostics = true;
});

builder.Services.AddConcurrencyMediatorBehavior();
```

---

## 4. Basic Domain Example

```csharp
public sealed class BankAccount : IVersionedEntity
{
    public string Id { get; init; } = string.Empty;
    public decimal Balance { get; set; }
    public long Version { get; set; }
}
```

```csharp
// Executing an in-memory CAS state transition
CasResult<BankAccount> outcome = await concurrencyController.ExecuteCasAsync(
    account,
    ExpectedVersion.Specific(10),
    account.Id,
    (acc, ct) =>
    {
        acc.Balance += 100m;
        return ValueTask.FromResult(acc);
    });

if (outcome.IsConflict)
{
    return Result.Failure(ConcurrencyErrors.FromConflict(outcome.Conflict!));
}
```
