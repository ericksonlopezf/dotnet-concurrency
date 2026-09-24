// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using Xunit;

namespace EricksonLopez.Concurrency.IntegrationTests;

public sealed class StressConcurrencyTests
{
    private sealed class CounterAggregate : IMutableVersionedEntity
    {
        public string Id { get; init; } = string.Empty;
        public int Counter { get; set; }
        public long Version { get; set; }
    }

    [Fact]
    public async Task HighContentionStressTest_100ConcurrentWriters_ShouldMaintainStrictInvariants()
    {
        var controller = new ConcurrencyController();
        var aggregate = new CounterAggregate { Id = "counter_1", Counter = 0, Version = 1 };

        const int totalWriters = 100;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var casResults = new ConcurrentBag<CasResult<CounterAggregate>>();

        Task[] tasks = Enumerable.Range(0, totalWriters).Select(i => Task.Run(async () =>
        {
            await startSignal.Task;

            CasResult<CounterAggregate> res = await controller.ExecuteCasAsync(
                aggregate,
                ExpectedVersion.Specific(1),
                aggregate.Id,
                (cnt, ct) =>
                {
                    cnt.Counter++;
                    return ValueTask.FromResult(cnt);
                });

            casResults.Add(res);
        })).ToArray();

        startSignal.SetResult();
        await Task.WhenAll(tasks);

        int successes = casResults.Count(r => r.IsSuccess);
        int conflicts = casResults.Count(r => r.IsConflict);

        (successes + conflicts).Should().Be(totalWriters);
        successes.Should().BeGreaterThanOrEqualTo(1);
        conflicts.Should().Be(totalWriters - successes);
    }

    [Fact]
    public async Task HighContentionCancellationStorm_ShouldNotLeakLocksAndAllowSubsequentOperations()
    {
        var controller = new ConcurrencyController();
        var aggregate = new CounterAggregate { Id = "counter_storm", Counter = 0, Version = 1 };

        const int totalWriters = 50;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();

        Task[] tasks = Enumerable.Range(0, totalWriters).Select(i => Task.Run(async () =>
        {
            await startSignal.Task;
            try
            {
                using var workerCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                if (i % 2 == 0)
                {
                    workerCts.CancelAfter(TimeSpan.FromMilliseconds(5));
                }

                await controller.ExecuteCasAsync(
                    aggregate,
                    ExpectedVersion.Specific(1),
                    aggregate.Id,
                    async (cnt, ct) =>
                    {
                        await Task.Delay(10, ct);
                        cnt.Counter++;
                        return cnt;
                    },
                    cancellationToken: workerCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected under cancellation storm
            }
            catch (TimeoutException)
            {
                // Expected if lock wait timed out
            }
        })).ToArray();

        startSignal.SetResult();
        cts.CancelAfter(TimeSpan.FromMilliseconds(25));

        await Task.WhenAll(tasks);

        // Verification: The lock for "counter_storm" is clean and non-leaked.
        var cleanAggregate = new CounterAggregate { Id = "counter_storm", Counter = 999, Version = 100 };
        CasResult<CounterAggregate> cleanResult = await controller.ExecuteCasAsync(
            cleanAggregate,
            ExpectedVersion.Specific(100),
            cleanAggregate.Id,
            (cnt, ct) => ValueTask.FromResult(cnt));

        cleanResult.IsSuccess.Should().BeTrue();
    }
}
