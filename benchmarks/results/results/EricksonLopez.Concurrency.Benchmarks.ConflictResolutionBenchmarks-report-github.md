```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                            | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| ResolveTransientConflictWithMerge | .NET 10.0 | .NET 10.0 | 115.2 ns | 0.89 ns | 0.74 ns |     ? |       ? | 0.0134 |     224 B |           ? |
| RejectFatalConflictImmediately    | .NET 10.0 | .NET 10.0 | 106.5 ns | 1.82 ns | 1.70 ns |     ? |       ? | 0.0129 |     216 B |           ? |
| ResolveTransientConflictWithMerge | .NET 8.0  | .NET 8.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |
| RejectFatalConflictImmediately    | .NET 8.0  | .NET 8.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |
| ResolveTransientConflictWithMerge | .NET 9.0  | .NET 9.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |
| RejectFatalConflictImmediately    | .NET 9.0  | .NET 9.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  ConflictResolutionBenchmarks.ResolveTransientConflictWithMerge: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConflictResolutionBenchmarks.RejectFatalConflictImmediately: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConflictResolutionBenchmarks.ResolveTransientConflictWithMerge: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConflictResolutionBenchmarks.RejectFatalConflictImmediately: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
