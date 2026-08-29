# Result Pattern Integration

## 1. Concurrency Errors

`EricksonLopez.Concurrency.Result` integrates seamlessly with `EricksonLopez.Result`, providing factory methods that produce typed `Error` instances with `ErrorType.Conflict` and structured diagnostic metadata:

```csharp
// Factory from structured conflict:
Error error = ConcurrencyErrors.FromConflict(conflict);

// Factory for version mismatch:
Error versionError = ConcurrencyErrors.VersionMismatch(
    entityId: "prod_1",
    entityType: "Product",
    expectedVersion: ExpectedVersion.Specific(10),
    actualVersion: ActualVersion.From(11));

// Factory for token mismatch:
Error tokenError = ConcurrencyErrors.TokenMismatch(
    entityId: "doc_10",
    entityType: "Document",
    expectedToken: new ConcurrencyToken("etag-1", "ETag"),
    actualToken: new ConcurrencyToken("etag-2", "ETag"));
```

---

## 2. Monadic Conversion Methods

| Source Type | Extension Method | Resulting Output |
|---|---|---|
| `CasResult<T>` | `.ToResult()` | `Result<T>.Success(entity)` or `Result<T>.Failure(ErrorType.Conflict)` |
| `ConflictResolution<T>` | `.ToResult()` | `Result<T>.Success(resolvedEntity)` or `Result<T>.Failure(ErrorType.Conflict)` |
| `int rowsAffected` | `ConcurrencyResultExtensions.FromRowsAffected(...)` | `Result.Success()` if `rows > 0`, else `Result.Failure(ErrorType.Conflict)` |
| `ConcurrencyConflict` | `.ToResult<T>()` | `Result<T>.Failure(ErrorType.Conflict)` with complete metadata payload |

---

## 3. Web API Status Code Mapping

When `Result<T>` fails with `ErrorType.Conflict`, API controllers or endpoint filters translate the error cleanly:
- `ErrorType.Conflict` $\rightarrow$ `HTTP 409 Conflict` (or `HTTP 412 Precondition Failed` if an If-Match token was supplied).
- Metadata fields (`expectedVersion`, `actualVersion`, `conflictType`) are included in the JSON `ProblemDetails` payload for client introspection.
