# OpenTelemetry & Observability

## 1. Diagnostics Infrastructure

`EricksonLopez.Concurrency` provides built-in OpenTelemetry instrumentation through `ConcurrencyDiagnostics`:
- **ActivitySource Name**: `"EricksonLopez.Concurrency"`
- **Meter Name**: `"EricksonLopez.Concurrency"`
- **Version**: `"1.0.0"`

---

## 2. Metric Instruments

| Metric Name | Type | Description | Tags / Dimensions |
|---|---|---|---|
| `concurrency.conflicts` | Counter (`long`) | Total number of detected concurrency conflicts. | `concurrency.conflict_type`, `concurrency.entity_type`, `concurrency.request` |
| `concurrency.successes` | Counter (`long`) | Total number of successful concurrency verifications / CAS operations. | `concurrency.entity_type` |
| `concurrency.failures` | Counter (`long`) | Total number of verification or database execution failures. | `concurrency.entity_type` |
| `concurrency.merges` | Counter (`long`) | Total number of successful three-way domain conflict merges. | `concurrency.strategy` |
| `concurrency.duration` | Histogram (`double`, ms) | Execution latency distribution for concurrency verifications and CAS operations. | `concurrency.operation` |

---

## 3. OpenTelemetry Distributed Tracing

Activities automatically track concurrency lifecycles:
- `concurrency.verify_version`: In-memory version validation.
- `concurrency.cas.execute`: Atomic Compare-And-Swap lifecycle.
- `concurrency.mediator.handle`: Mediator command interception.

### Activity Tags

| Tag Key | Example Value | Description |
|---|---|---|
| `concurrency.entity_id` | `"cust_12345"` | Identifier of the target entity. |
| `concurrency.entity_type` | `"CustomerAggregate"` | Type name of the target aggregate. |
| `concurrency.expected_version` | `"[Expected:10]"` | Expected version asserted by the caller. |
| `concurrency.actual_version` | `"[Actual:11]"` | Actual version observed during conflict. |
| `concurrency.conflict` | `true` | Boolean flag indicating whether a conflict occurred. |
| `concurrency.conflict_type` | `"VersionMismatch"` | Detailed conflict categorization. |

---

## 4. OpenTelemetry Registration Example

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("EricksonLopez.Concurrency")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("EricksonLopez.Concurrency")
        .AddOtlpExporter());
```
