```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                    | Job       | Runtime   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------- |---------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| DirectVersionComparison   | .NET 10.0 | .NET 10.0 |   0.1373 ns | 0.0038 ns | 0.0034 ns |     ? |       ? |      - |         - |           ? |
| CheckerCheckVersion       | .NET 10.0 | .NET 10.0 |   0.4691 ns | 0.0117 ns | 0.0110 ns |     ? |       ? |      - |         - |           ? |
| CheckerCheckToken         | .NET 10.0 | .NET 10.0 |  22.3628 ns | 0.2514 ns | 0.2352 ns |     ? |       ? | 0.0038 |      64 B |           ? |
| ControllerExecuteCasAsync | .NET 10.0 | .NET 10.0 | 182.8646 ns | 0.1885 ns | 0.1764 ns |     ? |       ? |      - |         - |           ? |
| ResultConversion          | .NET 10.0 | .NET 10.0 |   3.4495 ns | 0.0054 ns | 0.0048 ns |     ? |       ? |      - |         - |           ? |
| DirectVersionComparison   | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| CheckerCheckVersion       | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| CheckerCheckToken         | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| ControllerExecuteCasAsync | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| ResultConversion          | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| DirectVersionComparison   | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| CheckerCheckVersion       | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| CheckerCheckToken         | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| ControllerExecuteCasAsync | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
| ResultConversion          | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  ConcurrencyBenchmarks.DirectVersionComparison: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConcurrencyBenchmarks.CheckerCheckVersion: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConcurrencyBenchmarks.CheckerCheckToken: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConcurrencyBenchmarks.ControllerExecuteCasAsync: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConcurrencyBenchmarks.ResultConversion: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConcurrencyBenchmarks.DirectVersionComparison: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConcurrencyBenchmarks.CheckerCheckVersion: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConcurrencyBenchmarks.CheckerCheckToken: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConcurrencyBenchmarks.ControllerExecuteCasAsync: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConcurrencyBenchmarks.ResultConversion: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
