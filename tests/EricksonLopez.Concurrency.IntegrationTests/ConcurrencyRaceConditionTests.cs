// Copyright © Erickson Lopez. MIT License.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using Xunit;

namespace EricksonLopez.Concurrency.IntegrationTests;

public sealed class ConcurrencyRaceConditionTests
{
    private sealed class BankAccount : IVersionedEntity
    {
        public string Id { get; init; } = string.Empty;
        public decimal Balance { get; set; }
        public long Version { get; set; }
    }

    [Fact]
    public async Task ConcurrentWriters_SameVersion_OnlyOneShouldSucceedAndOthersConflict()
    {
        var controller = new ConcurrencyController();
        var account = new BankAccount { Id = "acc_100", Balance = 1000m, Version = 10 };

        const int writerCount = 10;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcomes = new ConcurrentBag<CasResult<BankAccount>>();

        Task[] tasks = Enumerable.Range(0, writerCount).Select(i => Task.Run(async () =>
        {
            await startSignal.Task;

            CasResult<BankAccount> outcome = await controller.ExecuteCasAsync(
                account,
                ExpectedVersion.Specific(10),
                account.Id,
                (acc, ct) =>
                {
                    acc.Balance += 10m;
                    return ValueTask.FromResult(acc);
                });

            outcomes.Add(outcome);
        })).ToArray();

        // Release all concurrent writers simultaneously
        startSignal.SetResult();
        await Task.WhenAll(tasks);

        int successes = outcomes.Count(o => o.IsSuccess);
        int conflicts = outcomes.Count(o => o.IsConflict);

        successes.Should().BeGreaterThanOrEqualTo(1);
        conflicts.Should().Be(writerCount - successes);
    }

    [Fact]
    public async Task SequentialUpdates_ShouldAdvanceVersionsDeterministically()
    {
        var controller = new ConcurrencyController();
        var account = new BankAccount { Id = "acc_seq", Balance = 100m, Version = 10 };

        for (long expected = 10; expected < 15; expected++)
        {
            CasResult<BankAccount> outcome = await controller.ExecuteCasAsync(
                account,
                ExpectedVersion.Specific(expected),
                account.Id,
                (acc, ct) =>
                {
                    acc.Balance += 20m;
                    acc.Version = expected + 1;
                    return ValueTask.FromResult(acc);
                });

            outcome.IsSuccess.Should().BeTrue();
            outcome.NewVersion.Should().Be(new ConcurrencyVersion(expected + 1));
        }

        account.Version.Should().Be(15);
        account.Balance.Should().Be(200m);
    }

    [Fact]
    public async Task ConcurrentUpdates_DifferentAggregates_ShouldAllSucceed()
    {
        var controller = new ConcurrencyController();
        const int count = 20;

        List<BankAccount> accounts = Enumerable.Range(1, count)
            .Select(i => new BankAccount { Id = $"acc_{i}", Balance = 100m, Version = 1 })
            .ToList();

        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var results = new ConcurrentBag<CasResult<BankAccount>>();

        Task[] tasks = accounts.Select(acc => Task.Run(async () =>
        {
            await startSignal.Task;

            CasResult<BankAccount> res = await controller.ExecuteCasAsync(
                acc,
                ExpectedVersion.Specific(1),
                acc.Id,
                (a, ct) =>
                {
                    a.Balance += 50m;
                    return ValueTask.FromResult(a);
                });

            results.Add(res);
        })).ToArray();

        startSignal.SetResult();
        await Task.WhenAll(tasks);

        results.Count(r => r.IsSuccess).Should().Be(count);
        results.Count(r => r.IsConflict).Should().Be(0);
    }
}
