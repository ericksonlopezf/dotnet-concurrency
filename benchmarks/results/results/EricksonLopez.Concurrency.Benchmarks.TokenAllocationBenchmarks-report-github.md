```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job       | Runtime   | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| CreateStructVersion           | .NET 10.0 | .NET 10.0 |  0.0002 ns | 0.0005 ns | 0.0004 ns |  0.0000 ns |     ? |       ? |      - |         - |           ? |
| CreateStructToken             | .NET 10.0 | .NET 10.0 |  1.1784 ns | 0.0024 ns | 0.0020 ns |  1.1790 ns |     ? |       ? |      - |         - |           ? |
| CreateExpectedVersionSpecific | .NET 10.0 | .NET 10.0 |  0.0001 ns | 0.0001 ns | 0.0001 ns |  0.0000 ns |     ? |       ? |      - |         - |           ? |
| TryParseSpanVersion           | .NET 10.0 | .NET 10.0 | 14.1884 ns | 0.0781 ns | 0.0731 ns | 14.2095 ns |     ? |       ? |      - |         - |           ? |
| TryParseStringVersion         | .NET 10.0 | .NET 10.0 | 16.1626 ns | 0.1534 ns | 0.1435 ns | 16.2476 ns |     ? |       ? |      - |         - |           ? |
| FormatVersionToString         | .NET 10.0 | .NET 10.0 | 13.6886 ns | 0.2392 ns | 0.2238 ns | 13.6255 ns |     ? |       ? | 0.0029 |      48 B |           ? |
| CompareTokensValueEquality    | .NET 10.0 | .NET 10.0 |  1.8755 ns | 0.0039 ns | 0.0037 ns |  1.8742 ns |     ? |       ? |      - |         - |           ? |
| CheckExpectedMatchesActual    | .NET 10.0 | .NET 10.0 |  0.3512 ns | 0.0042 ns | 0.0033 ns |  0.3515 ns |     ? |       ? |      - |         - |           ? |
| CreateStructVersion           | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CreateStructToken             | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CreateExpectedVersionSpecific | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| TryParseSpanVersion           | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| TryParseStringVersion         | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| FormatVersionToString         | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CompareTokensValueEquality    | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CheckExpectedMatchesActual    | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CreateStructVersion           | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CreateStructToken             | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CreateExpectedVersionSpecific | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| TryParseSpanVersion           | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| TryParseStringVersion         | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| FormatVersionToString         | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CompareTokensValueEquality    | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| CheckExpectedMatchesActual    | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  TokenAllocationBenchmarks.CreateStructVersion: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.CreateStructToken: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.CreateExpectedVersionSpecific: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.TryParseSpanVersion: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.TryParseStringVersion: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.FormatVersionToString: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.CompareTokensValueEquality: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.CheckExpectedMatchesActual: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  TokenAllocationBenchmarks.CreateStructVersion: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.CreateStructToken: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.CreateExpectedVersionSpecific: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.TryParseSpanVersion: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.TryParseStringVersion: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.FormatVersionToString: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.CompareTokensValueEquality: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  TokenAllocationBenchmarks.CheckExpectedMatchesActual: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
