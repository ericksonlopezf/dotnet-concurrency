
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4


 Method                            | Job       | Runtime   | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
---------------------------------- |---------- |---------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
 ResolveTransientConflictWithMerge | .NET 10.0 | .NET 10.0 | 89.87 ns | 0.215 ns | 0.202 ns |     ? |       ? | 0.0134 |     224 B |           ? |
 RejectFatalConflictImmediately    | .NET 10.0 | .NET 10.0 | 83.28 ns | 0.219 ns | 0.183 ns |     ? |       ? | 0.0129 |     216 B |           ? |
 ResolveTransientConflictWithMerge | .NET 8.0  | .NET 8.0  |       NA |       NA |       NA |     ? |       ? |     NA |        NA |           ? |
 RejectFatalConflictImmediately    | .NET 8.0  | .NET 8.0  |       NA |       NA |       NA |     ? |       ? |     NA |        NA |           ? |
 ResolveTransientConflictWithMerge | .NET 9.0  | .NET 9.0  |       NA |       NA |       NA |     ? |       ? |     NA |        NA |           ? |
 RejectFatalConflictImmediately    | .NET 9.0  | .NET 9.0  |       NA |       NA |       NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  ConflictResolutionBenchmarks.ResolveTransientConflictWithMerge: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConflictResolutionBenchmarks.RejectFatalConflictImmediately: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConflictResolutionBenchmarks.ResolveTransientConflictWithMerge: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConflictResolutionBenchmarks.RejectFatalConflictImmediately: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
