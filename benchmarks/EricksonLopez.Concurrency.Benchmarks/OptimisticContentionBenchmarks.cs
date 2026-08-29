// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;

namespace EricksonLopez.Concurrency.Benchmarks;

[MemoryDiagnoser]
public class OptimisticContentionBenchmarks
{
    public sealed class ContendedAccount : IVersionedEntity
    {
        public string Id { get; init; } = "acc-1001";
        public decimal Balance { get; set; } = 1000m;
        public long Version { get; set; } = 1;
    }

    private readonly ConcurrencyController _controller = new();

    [Benchmark(Baseline = true)]
    public async ValueTask<CasResult<ContendedAccount>> SingleWorkerUncontendedCas()
    {
        var account = new ContendedAccount { Version = 1 };
        return await _controller.ExecuteCasAsync(
            account,
            ExpectedVersion.Specific(1),
            account.Id,
            (acc, ct) =>
            {
                acc.Balance += 10m;
                return ValueTask.FromResult(acc);
            },
            CancellationToken.None);
    }

    [Benchmark]
    public async Task<int> ParallelContentionFourWorkers()
    {
        var account = new ContendedAccount { Version = 1 };
        int successCount = 0;

        Task[] tasks = new Task[4];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var result = await _controller.ExecuteCasAsync(
                    account,
                    ExpectedVersion.Specific(1),
                    account.Id,
                    (acc, ct) =>
                    {
                        acc.Balance += 10m;
                        return ValueTask.FromResult(acc);
                    },
                    CancellationToken.None);

                if (result.IsSuccess)
                {
                    Interlocked.Increment(ref successCount);
                }
            });
        }

        await Task.WhenAll(tasks);
        return successCount;
    }

    [Benchmark]
    public bool VersionPreconditionMismatchEvaluation()
    {
        var checker = OptimisticConcurrencyChecker.Instance;
        var expected = ExpectedVersion.Specific(5);
        var actual = new ConcurrencyVersion(10);

        return checker.CheckVersion(expected, actual, "acc-1001", "ContendedAccount", out _);
    }
}
