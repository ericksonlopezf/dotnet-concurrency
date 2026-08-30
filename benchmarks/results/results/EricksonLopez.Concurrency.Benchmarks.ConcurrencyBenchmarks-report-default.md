
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


 Method                    | Job       | Runtime   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
-------------------------- |---------- |---------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
 DirectVersionComparison   | .NET 10.0 | .NET 10.0 |   0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |      - |         - |           ? |
 CheckerCheckVersion       | .NET 10.0 | .NET 10.0 |   0.5392 ns | 0.0090 ns | 0.0075 ns |     ? |       ? |      - |         - |           ? |
 CheckerCheckToken         | .NET 10.0 | .NET 10.0 |  22.3995 ns | 0.4488 ns | 0.4408 ns |     ? |       ? | 0.0038 |      64 B |           ? |
 ControllerExecuteCasAsync | .NET 10.0 | .NET 10.0 | 184.7967 ns | 0.6507 ns | 0.5433 ns |     ? |       ? |      - |         - |           ? |
 ResultConversion          | .NET 10.0 | .NET 10.0 |   4.0744 ns | 0.0103 ns | 0.0096 ns |     ? |       ? |      - |         - |           ? |
 DirectVersionComparison   | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckVersion       | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckToken         | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 ControllerExecuteCasAsync | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 ResultConversion          | .NET 8.0  | .NET 8.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 DirectVersionComparison   | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckVersion       | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 CheckerCheckToken         | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 ControllerExecuteCasAsync | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |
 ResultConversion          | .NET 9.0  | .NET 9.0  |          NA |        NA |        NA |     ? |       ? |     NA |        NA |           ? |

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
