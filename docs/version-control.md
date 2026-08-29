# Version Control & Semantics

## 1. Version Value Objects

`EricksonLopez.Concurrency` provides two strongly-typed version representations:
1. `ConcurrencyVersion`: A lightweight `readonly record struct` encapsulating a `long` value $\ge 0$.
2. `ConcurrencyVersion<TEntity>`: A generic wrapper preventing accidental assignment between disparate aggregates (e.g., comparing an `Order` version against a `Customer` version at compile time).

```csharp
var v0 = ConcurrencyVersion.None;    // Value = 0 (Unset/New)
var v1 = ConcurrencyVersion.Initial; // Value = 1 (First persistent state)
var next = v1.Next();                // Value = 2
```

---

## 2. ExpectedVersion Discriminators

`ExpectedVersion` represents the caller's optimistic expectation regarding the target entity state:

| Discriminator | Creation Method | Behavior & Invariants |
|---|---|---|
| **Specific** | `ExpectedVersion.Specific(10)` | Matches only when the actual version is exactly 10. Fails if version differs or entity does not exist. |
| **Any** | `ExpectedVersion.Any` | Bypasses version validation; unconditionally matches any existing or new state. |
| **New** | `ExpectedVersion.New` | Asserts that the entity does not yet exist (`version == 0` or `None`). Fails if entity exists. |
| **Exists** | `ExpectedVersion.Exists` | Asserts that the entity already exists (`version > 0`). Fails if version is 0. |

```csharp
ExpectedVersion expected = ExpectedVersion.Specific(5);
bool matches = expected.Matches(new ConcurrencyVersion(5)); // true
bool isMismatch = expected.Matches(new ConcurrencyVersion(6)); // false
```

---

## 3. ActualVersion Semantics

`ActualVersion` captures the actual state observed in storage or domain memory:
- `ActualVersion.From(long version)`: Entity exists with the specified version.
- `ActualVersion.NotFound`: Entity does not exist in the database or store.

This allows distinguishing between a **Version Mismatch** (entity exists with a different version) and a **State Deleted** conflict (entity was removed by another process).
