```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4


```
| Method                                | Job       | Runtime   | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |---------- |---------- |-----------:|---------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| SingleWorkerUncontendedCas            | .NET 10.0 | .NET 10.0 |   175.5 ns |  0.27 ns |   0.23 ns |   175.4 ns |     ? |       ? | 0.0029 |      48 B |           ? |
| ParallelContentionFourWorkers         | .NET 10.0 | .NET 10.0 | 2,187.7 ns | 43.10 ns | 117.27 ns | 2,149.4 ns |     ? |       ? | 0.0572 |     976 B |           ? |
| VersionPreconditionMismatchEvaluation | .NET 10.0 | .NET 10.0 |   184.7 ns |  1.15 ns |   1.08 ns |   184.8 ns |     ? |       ? | 0.0339 |     568 B |           ? |
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
