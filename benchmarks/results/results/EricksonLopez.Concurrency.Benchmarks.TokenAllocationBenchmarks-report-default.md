
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


 Method                        | Job       | Runtime   | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
 CreateStructVersion           | .NET 10.0 | .NET 10.0 |  0.0006 ns | 0.0011 ns | 0.0010 ns |  0.0002 ns |     ? |       ? |      - |         - |           ? |
 CreateStructToken             | .NET 10.0 | .NET 10.0 |  1.1816 ns | 0.0051 ns | 0.0046 ns |  1.1807 ns |     ? |       ? |      - |         - |           ? |
 CreateExpectedVersionSpecific | .NET 10.0 | .NET 10.0 |  0.0002 ns | 0.0005 ns | 0.0005 ns |  0.0000 ns |     ? |       ? |      - |         - |           ? |
 TryParseSpanVersion           | .NET 10.0 | .NET 10.0 | 14.1844 ns | 0.0593 ns | 0.0463 ns | 14.1906 ns |     ? |       ? |      - |         - |           ? |
 TryParseStringVersion         | .NET 10.0 | .NET 10.0 | 16.1672 ns | 0.1433 ns | 0.1340 ns | 16.1973 ns |     ? |       ? |      - |         - |           ? |
 FormatVersionToString         | .NET 10.0 | .NET 10.0 | 13.9879 ns | 0.2585 ns | 0.2418 ns | 14.0261 ns |     ? |       ? | 0.0029 |      48 B |           ? |
 CompareTokensValueEquality    | .NET 10.0 | .NET 10.0 |  1.8762 ns | 0.0057 ns | 0.0047 ns |  1.8761 ns |     ? |       ? |      - |         - |           ? |
 CheckExpectedMatchesActual    | .NET 10.0 | .NET 10.0 |  0.3431 ns | 0.0051 ns | 0.0043 ns |  0.3435 ns |     ? |       ? |      - |         - |           ? |
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
