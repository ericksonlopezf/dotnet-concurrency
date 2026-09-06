
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4


 Method                        | Job       | Runtime   | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
 CreateStructVersion           | .NET 10.0 | .NET 10.0 |  0.0016 ns | 0.0025 ns | 0.0022 ns |  0.0006 ns |     ? |       ? |      - |         - |           ? |
 CreateStructToken             | .NET 10.0 | .NET 10.0 |  1.0244 ns | 0.0026 ns | 0.0022 ns |  1.0236 ns |     ? |       ? |      - |         - |           ? |
 CreateExpectedVersionSpecific | .NET 10.0 | .NET 10.0 |  0.0002 ns | 0.0004 ns | 0.0004 ns |  0.0000 ns |     ? |       ? |      - |         - |           ? |
 TryParseSpanVersion           | .NET 10.0 | .NET 10.0 | 12.1846 ns | 0.0082 ns | 0.0068 ns | 12.1826 ns |     ? |       ? |      - |         - |           ? |
 TryParseStringVersion         | .NET 10.0 | .NET 10.0 | 13.8142 ns | 0.0044 ns | 0.0039 ns | 13.8133 ns |     ? |       ? |      - |         - |           ? |
 FormatVersionToString         | .NET 10.0 | .NET 10.0 | 10.6876 ns | 0.1724 ns | 0.1528 ns | 10.6473 ns |     ? |       ? | 0.0029 |      48 B |           ? |
 CompareTokensValueEquality    | .NET 10.0 | .NET 10.0 |  1.5350 ns | 0.0015 ns | 0.0013 ns |  1.5348 ns |     ? |       ? |      - |         - |           ? |
 CheckExpectedMatchesActual    | .NET 10.0 | .NET 10.0 |  0.2734 ns | 0.0011 ns | 0.0010 ns |  0.2732 ns |     ? |       ? |      - |         - |           ? |
 CreateStructVersion           | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CreateStructToken             | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CreateExpectedVersionSpecific | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 TryParseSpanVersion           | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 TryParseStringVersion         | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 FormatVersionToString         | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CompareTokensValueEquality    | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CheckExpectedMatchesActual    | .NET 8.0  | .NET 8.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CreateStructVersion           | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CreateStructToken             | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CreateExpectedVersionSpecific | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 TryParseSpanVersion           | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 TryParseStringVersion         | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 FormatVersionToString         | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CompareTokensValueEquality    | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
 CheckExpectedMatchesActual    | .NET 9.0  | .NET 9.0  |         NA |        NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |

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
