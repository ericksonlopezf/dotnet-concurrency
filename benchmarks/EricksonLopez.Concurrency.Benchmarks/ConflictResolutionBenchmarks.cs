// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Resolvers;

namespace EricksonLopez.Concurrency.Benchmarks;

[MemoryDiagnoser]
public class ConflictResolutionBenchmarks
{
    public sealed class OrderAggregate : IVersionedEntity
    {
        public string Id { get; init; } = "order-555";
        public decimal TotalAmount { get; set; } = 250m;
        public long Version { get; set; } = 3;
    }

    private readonly RefreshAndRetryConflictResolver<OrderAggregate> _resolver;
    private readonly ConcurrencyConflict _transientConflict;
    private readonly ConcurrencyConflict _fatalConflict;
    private readonly OrderAggregate _latestEntity;

    public ConflictResolutionBenchmarks()
    {
        _latestEntity = new OrderAggregate { Version = 4, TotalAmount = 260m };
        _resolver = new RefreshAndRetryConflictResolver<OrderAggregate>(
            refreshDelegate: (id, ct) => ValueTask.FromResult<OrderAggregate?>(_latestEntity),
            reapplyDelegate: (proposed, current) =>
            {
                current.TotalAmount = proposed.TotalAmount + 10m;
                return current;
            });

        _transientConflict = ConcurrencyConflict.VersionMismatch(
            "order-555",
            "OrderAggregate",
            ExpectedVersion.Specific(3),
            new ActualVersion(4));

        _fatalConflict = new ConcurrencyConflict(
            "order-555",
            "OrderAggregate",
            ConcurrencyConflictType.StateDeleted,
            ConcurrencyConflictClassification.Fatal,
            "Delete",
            "Entity was permanently deleted");
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<ConflictResolution<OrderAggregate>> ResolveTransientConflictWithMerge()
    {
        var proposed = new OrderAggregate { Version = 3, TotalAmount = 250m };
        return await _resolver.ResolveAsync(proposed, _latestEntity, _transientConflict, CancellationToken.None);
    }

    [Benchmark]
    public async ValueTask<ConflictResolution<OrderAggregate>> RejectFatalConflictImmediately()
    {
        var proposed = new OrderAggregate { Version = 1, TotalAmount = 100m };
        return await _resolver.ResolveAsync(proposed, null, _fatalConflict, CancellationToken.None);
    }
}
