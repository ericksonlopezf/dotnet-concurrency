
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4


 Method                    | Job       | Runtime   | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
-------------------------- |---------- |---------- |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
 DirectVersionComparison   | .NET 10.0 | .NET 10.0 |   0.0017 ns | 0.0035 ns | 0.0033 ns |   0.0000 ns |     ? |       ? |      - |         - |           ? |
 CheckerCheckVersion       | .NET 10.0 | .NET 10.0 |   0.2740 ns | 0.0021 ns | 0.0017 ns |   0.2736 ns |     ? |       ? |      - |         - |           ? |
 CheckerCheckToken         | .NET 10.0 | .NET 10.0 |  18.2191 ns | 0.3640 ns | 0.3405 ns |  18.3029 ns |     ? |       ? | 0.0038 |      64 B |           ? |
 ControllerExecuteCasAsync | .NET 10.0 | .NET 10.0 | 156.5198 ns | 0.1203 ns | 0.1125 ns | 156.5326 ns |     ? |       ? |      - |         - |           ? |
 ResultConversion          | .NET 10.0 | .NET 10.0 |   3.1047 ns | 0.0667 ns | 0.1132 ns |   3.0415 ns |     ? |       ? |      - |         - |           ? |
 DirectVersionComparison   | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckVersion       | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckToken         | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 ControllerExecuteCasAsync | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 ResultConversion          | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 DirectVersionComparison   | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckVersion       | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckToken         | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 ControllerExecuteCasAsync | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |
 ResultConversion          | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |          NA |     ? |       ? |     NA |        NA |           ? |

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
