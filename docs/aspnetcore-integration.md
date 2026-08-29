# ASP.NET Core Integration (`EricksonLopez.Concurrency.AspNetCore`)

The `EricksonLopez.Concurrency.AspNetCore` package provides out-of-the-box support for REST APIs, RFC 7807 `ProblemDetails` translation, HTTP 409 Conflict status codes, HTTP `ETag` headers, and Minimal API results.

---

## 1. Installation

```bash
dotnet add package EricksonLopez.Concurrency.AspNetCore
```

---

## 2. Automatic Exception-to-409 Middleware

Register the middleware in `Program.cs` to intercept `ConcurrencyException` and automatically output RFC 7807 ProblemDetails with HTTP 409:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEricksonLopezConcurrency();
builder.Services.AddConcurrencyAspNetCore();

var app = builder.Build();

// Register concurrency conflict handling middleware early in the pipeline
app.UseConcurrencyConflictHandling();

app.MapControllers();
app.Run();
```

When a `ConcurrencyException` is thrown downstream, the middleware generates:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
ETag: W/"6"

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Concurrency Conflict: VersionMismatch",
  "status": 409,
  "detail": "Expected version 5, but discovered actual version 6.",
  "instance": "/api/products/prod-42",
  "conflictType": "VersionMismatch",
  "classification": "Transient",
  "entityId": "prod-42",
  "entityType": "Product",
  "expectedVersion": "5",
  "actualVersion": "6"
}
```

---

## 3. Minimal API Endpoint Extensions

If you prefer functional monadic responses without throwing exceptions, use `Results.Extensions.ConcurrencyConflict`:

```csharp
app.MapPut("/api/products/{id}", async (
    string id,
    UpdateProductRequest request,
    IConcurrencyController controller,
    IProductRepository repository,
    CancellationToken ct) =>
{
    Product? product = await repository.GetByIdAsync(id, ct);
    if (product is null) return Results.NotFound();

    var expectedVersion = ExpectedVersion.Specific(request.Version);
    var conflict = controller.VerifyVersion(product, expectedVersion, id);
    if (conflict is not null)
    {
        return Results.Extensions.ConcurrencyConflict(conflict);
    }

    // Persist changes...
    return Results.Ok(product);
});
```

---

## 4. HTTP ETag & Precondition Header Helpers

The package includes extensions for reading and writing `If-Match`, `If-None-Match`, and `ETag` headers:

### 4.1 Extracting Expected Version from `If-Match`

```csharp
app.MapPut("/api/orders/{id}", async (string id, HttpRequest request, IConcurrencyController controller) =>
{
    // Extracts numeric version from `If-Match: "5"` or `If-Match: W/"5"`
    ExpectedVersion? expected = request.GetExpectedConcurrencyVersion();
    
    // Or extract as opaque string token:
    ConcurrencyToken? token = request.GetExpectedConcurrencyToken();

    // Verify...
});
```

### 4.2 Setting `ETag` Response Headers

```csharp
app.MapGet("/api/products/{id}", async (string id, HttpResponse response, IProductRepository repo) =>
{
    Product product = await repo.GetByIdAsync(id);

    // Sets ETag: W/"12" on response
    response.SetConcurrencyETag(product.Version, isWeak: true);

    return Results.Ok(product);
});
```
