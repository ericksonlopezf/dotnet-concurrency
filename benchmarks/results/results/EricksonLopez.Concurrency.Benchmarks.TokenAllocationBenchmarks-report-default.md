
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


 Method                        | Job       | Runtime   | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
 CreateStructVersion           | .NET 10.0 | .NET 10.0 |  0.0002 ns | 0.0006 ns | 0.0005 ns |  0.0000 ns |     ? |       ? |      - |         - |           ? |
 CreateStructToken             | .NET 10.0 | .NET 10.0 |  1.1799 ns | 0.0029 ns | 0.0026 ns |  1.1797 ns |     ? |       ? |      - |         - |           ? |
 CreateExpectedVersionSpecific | .NET 10.0 | .NET 10.0 |  0.0005 ns | 0.0007 ns | 0.0006 ns |  0.0004 ns |     ? |       ? |      - |         - |           ? |
 TryParseSpanVersion           | .NET 10.0 | .NET 10.0 | 14.1848 ns | 0.0335 ns | 0.0297 ns | 14.1850 ns |     ? |       ? |      - |         - |           ? |
 TryParseStringVersion         | .NET 10.0 | .NET 10.0 | 16.2165 ns | 0.1586 ns | 0.1484 ns | 16.2747 ns |     ? |       ? |      - |         - |           ? |
 FormatVersionToString         | .NET 10.0 | .NET 10.0 | 14.0170 ns | 0.0952 ns | 0.0890 ns | 14.0460 ns |     ? |       ? | 0.0029 |      48 B |           ? |
 CompareTokensValueEquality    | .NET 10.0 | .NET 10.0 |  1.8836 ns | 0.0043 ns | 0.0038 ns |  1.8838 ns |     ? |       ? |      - |         - |           ? |
 CheckExpectedMatchesActual    | .NET 10.0 | .NET 10.0 |  0.3750 ns | 0.0022 ns | 0.0018 ns |  0.3752 ns |     ? |       ? |      - |         - |           ? |
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
