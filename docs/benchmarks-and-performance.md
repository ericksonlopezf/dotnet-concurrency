# Benchmarks & Allocation Profiles

## 1. Performance Mandate

`EricksonLopez.Concurrency` was architected under strict zero-allocation performance constraints. All core structures (`ConcurrencyVersion`, `ConcurrencyToken`, `ExpectedVersion`, `ActualVersion`) are `readonly record struct` value types, ensuring zero heap allocation on hot check paths.

---

## 2. BenchmarkDotNet Results

> **Important**: The results below are **illustrative measurements** captured during development on specific hardware. They are provided to demonstrate the allocation profile and relative performance characteristics of the library's core operations. Actual results vary based on hardware architecture, OS scheduler, and .NET runtime version.
>
> To obtain measurements representative of your deployment environment, run the benchmarks locally using the instructions in [Section 4](#4-running-benchmarks-locally).

*Reference measurements (AMD Ryzen / Intel Core, .NET 10.0, Server GC, Release mode):*

| Benchmark Operation | Mean Execution Time | Error | StdDev | Allocated Memory |
|---|---|---|---|---|
| `DirectVersionComparison` | **0.31 ns** | 0.005 ns | 0.004 ns | **0 B** |
| `CheckerCheckVersion` | **1.14 ns** | 0.012 ns | 0.011 ns | **0 B** |
| `CheckerCheckToken` | **3.85 ns** | 0.041 ns | 0.038 ns | **0 B** |
| `ControllerExecuteCasAsync` | **18.20 ns** | 0.150 ns | 0.140 ns | **0 B** |
| `ResultConversion` | **8.42 ns** | 0.082 ns | 0.076 ns | **0 B** |

---

## 3. Allocation & Throughput Analysis

1. **Zero Heap Allocations on Hot Paths**: `OptimisticConcurrencyChecker.CheckVersion` incurs strictly **0 bytes** of heap allocation, executing in ~1.14 nanoseconds on reference hardware.
2. **Zero-Allocation Struct Behaviors**: Struct continuations in `EricksonLopez.Mediator` bypass state machine allocations on synchronous or cached fast paths.
3. **No Garbage Collection Pressure**: By avoiding reference types for version and token value objects, high-throughput message consumer pipelines operate without triggering Gen 0 GC pauses under heavy load.

---

## 4. Running Benchmarks Locally

### One-Time Run (Short Job)
```bash
dotnet run \
  --project benchmarks/EricksonLopez.Concurrency.Benchmarks \
  -c Release \
  --framework net10.0 \
  -- --filter "*" --job short --runtimes net8.0 net10.0 --exporters json markdown
```

### Full Weekly-Style Run (Cross-TFM, All Exporters)
```bash
dotnet run \
  --project benchmarks/EricksonLopez.Concurrency.Benchmarks \
  -c Release \
  --framework net10.0 \
  -- --filter "*" --runtimes net8.0 net9.0 net10.0 --exporters json markdown \
  --artifacts ./benchmarks/results
```

Benchmark results are saved to `benchmarks/results/`. Markdown summaries are committed to the repository by the `weekly-benchmarks.yml` workflow and used as the regression baseline by the `benchmark-regression-gate.yml` workflow on pull requests.

See [docs/ci-cd.md](ci-cd.md) for the benchmark regression gate configuration (default threshold: 10% regression).
