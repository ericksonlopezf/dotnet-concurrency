```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                                | Job       | Runtime   | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |---------- |---------- |-----------:|---------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| SingleWorkerUncontendedCas            | .NET 10.0 | .NET 10.0 |   198.8 ns |  0.25 ns |   0.22 ns |   198.8 ns |     ? |       ? | 0.0029 |      48 B |           ? |
| ParallelContentionFourWorkers         | .NET 10.0 | .NET 10.0 | 2,692.6 ns | 59.78 ns | 167.62 ns | 2,630.8 ns |     ? |       ? | 0.0534 |     976 B |           ? |
| VersionPreconditionMismatchEvaluation | .NET 10.0 | .NET 10.0 |   232.7 ns |  1.33 ns |   1.11 ns |   233.0 ns |     ? |       ? | 0.0339 |     568 B |           ? |
| SingleWorkerUncontendedCas            | .NET 8.0  | .NET 8.0  |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| ParallelContentionFourWorkers         | .NET 8.0  | .NET 8.0  |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| VersionPreconditionMismatchEvaluation | .NET 8.0  | .NET 8.0  |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| SingleWorkerUncontendedCas            | .NET 9.0  | .NET 9.0  |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| ParallelContentionFourWorkers         | .NET 9.0  | .NET 9.0  |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| VersionPreconditionMismatchEvaluation | .NET 9.0  | .NET 9.0  |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  OptimisticContentionBenchmarks.SingleWorkerUncontendedCas: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  OptimisticContentionBenchmarks.ParallelContentionFourWorkers: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  OptimisticContentionBenchmarks.VersionPreconditionMismatchEvaluation: .NET 8.0(Runtime=.NET 8.0, Toolchain=net8.0)
  OptimisticContentionBenchmarks.SingleWorkerUncontendedCas: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  OptimisticContentionBenchmarks.ParallelContentionFourWorkers: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
  OptimisticContentionBenchmarks.VersionPreconditionMismatchEvaluation: .NET 9.0(Runtime=.NET 9.0, Toolchain=net9.0)
