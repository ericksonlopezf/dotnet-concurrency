
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


 Method                            | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
---------------------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
 ResolveTransientConflictWithMerge | .NET 10.0 | .NET 10.0 | 112.2 ns | 0.98 ns | 0.92 ns |     ? |       ? | 0.0134 |     224 B |           ? |
 RejectFatalConflictImmediately    | .NET 10.0 | .NET 10.0 | 108.8 ns | 1.41 ns | 1.18 ns |     ? |       ? | 0.0129 |     216 B |           ? |
 ResolveTransientConflictWithMerge | .NET 8.0  | .NET 8.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |
 RejectFatalConflictImmediately    | .NET 8.0  | .NET 8.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |
 ResolveTransientConflictWithMerge | .NET 9.0  | .NET 9.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |
 RejectFatalConflictImmediately    | .NET 9.0  | .NET 9.0  |       NA |      NA |      NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  ConflictResolutionBenchmarks.ResolveTransientConflictWithMerge: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConflictResolutionBenchmarks.RejectFatalConflictImmediately: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  ConflictResolutionBenchmarks.ResolveTransientConflictWithMerge: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  ConflictResolutionBenchmarks.RejectFatalConflictImmediately: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
