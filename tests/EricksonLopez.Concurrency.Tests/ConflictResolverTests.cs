// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;
using EricksonLopez.Concurrency.Resolvers;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class ConflictResolverTests
{
    private sealed class ResolverAccount
    {
        public string Id { get; init; } = string.Empty;
        public decimal Balance { get; set; }
    }

    [Fact]
    public async Task RejectConflictResolver_ShouldAlwaysReject()
    {
        var resolver = RejectConflictResolver<ResolverAccount>.Instance;
        var proposed = new ResolverAccount { Id = "c1", Balance = 100 };
        var currentDb = new ResolverAccount { Id = "c1", Balance = 150 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, currentDb, conflict, CancellationToken.None);

        resolution.IsResolved.Should().BeFalse();
        resolution.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
        resolution.ResolvedEntity.Should().BeNull();
        resolution.Reason.Should().Be(conflict.Message);

        // With null conflict
        ConflictResolution<ResolverAccount> resolutionNullConflict = await resolver.ResolveAsync(proposed, currentDb, null!, CancellationToken.None);
        resolutionNullConflict.Reason.Should().Be("Conflict rejected by policy.");
    }

    [Fact]
    public async Task LastWriteWinsConflictResolver_ShouldReturnProposedEntityAndRecordMerge()
    {
        long mergeCount = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.merges")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type" && tag.Value?.ToString() == nameof(ResolverAccount))
                    {
                        mergeCount += measurement;
                    }
                }
            }
        });
        meterListener.Start();

        var resolver = LastWriteWinsConflictResolver<ResolverAccount>.Instance;
        var proposed = new ResolverAccount { Id = "c1", Balance = 200 };
        var currentDb = new ResolverAccount { Id = "c1", Balance = 150 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, currentDb, conflict, CancellationToken.None);

        resolution.IsResolved.Should().BeTrue();
        resolution.Strategy.Should().Be(ConflictResolutionStrategy.LastWriteWinsExplicit);
        resolution.ResolvedEntity.Should().Be(proposed);
        resolution.Reason.Should().Be("Explicit Last-Write-Wins overwrite strategy applied.");
        mergeCount.Should().Be(1);
    }

    [Fact]
    public async Task LastWriteWinsConflictResolver_NullProposed_ShouldThrow()
    {
        var resolver = LastWriteWinsConflictResolver<ResolverAccount>.Instance;
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        var act = async () => await resolver.ResolveAsync(null!, null, conflict);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("proposedEntity");
    }

    [Theory]
    [InlineData(ConflictResolutionStrategy.MergeDomainSpecific)]
    [InlineData(ConflictResolutionStrategy.RefreshAndRetry)]
    [InlineData(ConflictResolutionStrategy.LastWriteWinsExplicit)]
    public async Task DelegateConflictResolver_MergeStrategies_ShouldRecordMerge(ConflictResolutionStrategy strategy)
    {
        long mergeCount = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.merges")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type" && tag.Value?.ToString() == nameof(ResolverAccount))
                    {
                        mergeCount += measurement;
                    }
                }
            }
        });
        meterListener.Start();

        var resolver = new DelegateConflictResolver<ResolverAccount>((prop, db, conf, ct) =>
        {
            var merged = new ResolverAccount { Id = prop.Id, Balance = 999 };
            return ValueTask.FromResult(new ConflictResolution<ResolverAccount>(merged, strategy, "Custom resolved."));
        });

        var proposed = new ResolverAccount { Id = "c1", Balance = 50 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, null, conflict);
        resolution.IsResolved.Should().BeTrue();
        resolution.Strategy.Should().Be(strategy);
        mergeCount.Should().Be(1);
    }

    [Fact]
    public async Task DelegateConflictResolver_RejectStrategy_ShouldNotRecordMerge()
    {
        long mergeCount = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.merges")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type" && tag.Value?.ToString() == nameof(ResolverAccount))
                    {
                        mergeCount += measurement;
                    }
                }
            }
        });
        meterListener.Start();

        var resolver = new DelegateConflictResolver<ResolverAccount>((prop, db, conf, ct) =>
            ValueTask.FromResult(ConflictResolution.Rejected<ResolverAccount>("Delegate rejected.")));

        var proposed = new ResolverAccount { Id = "c1", Balance = 50 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, null, conflict);
        resolution.IsResolved.Should().BeFalse();
        resolution.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
        mergeCount.Should().Be(0);
    }

    [Fact]
    public async Task DelegateConflictResolver_NullGuards()
    {
        Action actCtor = () => _ = new DelegateConflictResolver<ResolverAccount>(null!);
        actCtor.Should().Throw<ArgumentNullException>()
            .WithParameterName("resolveDelegate");

        var resolver = new DelegateConflictResolver<ResolverAccount>((prop, db, conf, ct) =>
            ValueTask.FromResult(ConflictResolution.Rejected<ResolverAccount>()));

        var conflict = ConcurrencyConflict.Deleted("c1", "ResolverAccount");
        var proposed = new ResolverAccount { Id = "c1" };

        var actNullProposed = async () => await resolver.ResolveAsync(null!, null, conflict);
        await actNullProposed.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("proposedEntity");

        var actNullConflict = async () => await resolver.ResolveAsync(proposed, null, null!);
        await actNullConflict.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("conflict");
    }

    [Fact]
    public void RefreshAndRetryConflictResolver_ConstructorValidation()
    {
        // maxRetries = 1 is valid (must not throw)
        var validResolver1 = new RefreshAndRetryConflictResolver<ResolverAccount>((id, ct) => ValueTask.FromResult<ResolverAccount?>(null), 1);
        validResolver1.Should().NotBeNull();

        var validResolver2 = new RefreshAndRetryConflictResolver<ResolverAccount>((id, ct) => ValueTask.FromResult<ResolverAccount?>(null), (p, l) => p, 1);
        validResolver2.Should().NotBeNull();

        Action actNull1 = () => _ = new RefreshAndRetryConflictResolver<ResolverAccount>(null!);
        actNull1.Should().Throw<ArgumentNullException>().WithParameterName("refreshDelegate");

        Action actRetries1 = () => _ = new RefreshAndRetryConflictResolver<ResolverAccount>((id, ct) => ValueTask.FromResult<ResolverAccount?>(null), 0);
        actRetries1.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Maximum retries must be at least 1.*")
            .WithParameterName("maxRetries");

        Action actNull2A = () => _ = new RefreshAndRetryConflictResolver<ResolverAccount>(null!, (p, l) => p);
        actNull2A.Should().Throw<ArgumentNullException>().WithParameterName("refreshDelegate");

        Action actNull2B = () => _ = new RefreshAndRetryConflictResolver<ResolverAccount>((id, ct) => ValueTask.FromResult<ResolverAccount?>(null), null!);
        actNull2B.Should().Throw<ArgumentNullException>().WithParameterName("reapplyDelegate");

        Action actRetries2 = () => _ = new RefreshAndRetryConflictResolver<ResolverAccount>((id, ct) => ValueTask.FromResult<ResolverAccount?>(null), (p, l) => p, 0);
        actRetries2.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Maximum retries must be at least 1.*")
            .WithParameterName("maxRetries");
    }

    [Fact]
    public async Task RefreshAndRetryConflictResolver_ShouldReloadAndResolve_WithReapplyDelegate()
    {
        long mergeCount = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.merges")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type" && tag.Value?.ToString() == nameof(ResolverAccount))
                    {
                        mergeCount += measurement;
                    }
                }
            }
        });
        meterListener.Start();

        var dbEntity = new ResolverAccount { Id = "c1", Balance = 250 };
        var resolver = new RefreshAndRetryConflictResolver<ResolverAccount>(
            refreshDelegate: (id, ct) => ValueTask.FromResult<ResolverAccount?>(dbEntity),
            reapplyDelegate: (prop, latest) => new ResolverAccount { Id = latest.Id, Balance = latest.Balance + prop.Balance },
            maxRetries: 3);

        var proposed = new ResolverAccount { Id = "c1", Balance = 50 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, null, conflict, CancellationToken.None);

        resolution.IsResolved.Should().BeTrue();
        resolution.Strategy.Should().Be(ConflictResolutionStrategy.RefreshAndRetry);
        resolution.ResolvedEntity.Should().NotBeNull();
        resolution.ResolvedEntity!.Balance.Should().Be(300);
        resolution.Reason.Should().Be("State refreshed from storage and reconciled with strategy RefreshAndRetry.");
        mergeCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAndRetryConflictResolver_ShouldNotCallRefreshDelegate_WhenCurrentDbEntityProvided()
    {
        bool refreshCalled = false;
        var directDbEntity = new ResolverAccount { Id = "c1", Balance = 100 };
        var resolver = new RefreshAndRetryConflictResolver<ResolverAccount>(
            refreshDelegate: (id, ct) =>
            {
                refreshCalled = true;
                return ValueTask.FromResult<ResolverAccount?>(new ResolverAccount { Id = "other", Balance = 999 });
            });

        var proposed = new ResolverAccount { Id = "c1", Balance = 50 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        // When currentDatabaseEntity is not null, refreshDelegate MUST NOT be called
        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, directDbEntity, conflict);
        resolution.IsResolved.Should().BeTrue();
        resolution.ResolvedEntity.Should().BeSameAs(directDbEntity);
        refreshCalled.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAndRetryConflictResolver_ShouldReject_WhenClassificationIsNonRetryableOrFatal()
    {
        var resolver = new RefreshAndRetryConflictResolver<ResolverAccount>(
            refreshDelegate: (id, ct) => ValueTask.FromResult<ResolverAccount?>(new ResolverAccount { Id = id, Balance = 100 }));

        var proposed = new ResolverAccount { Id = "c1", Balance = 50 };

        var nonRetryableConflict = new ConcurrencyConflict(
            "c1",
            "ResolverAccount",
            ConcurrencyConflictType.StateDeleted,
            ConcurrencyConflictClassification.NonRetryable,
            "Update",
            "Entity was deleted.");

        ConflictResolution<ResolverAccount> resNonRetryable = await resolver.ResolveAsync(proposed, null, nonRetryableConflict, CancellationToken.None);
        resNonRetryable.IsResolved.Should().BeFalse();
        resNonRetryable.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
        resNonRetryable.Reason.Should().Contain("cannot be refreshed");

        var fatalConflict = new ConcurrencyConflict(
            "c1",
            "ResolverAccount",
            ConcurrencyConflictType.Custom,
            ConcurrencyConflictClassification.Fatal,
            "Update",
            "Fatal corruption.");

        ConflictResolution<ResolverAccount> resFatal = await resolver.ResolveAsync(proposed, null, fatalConflict, CancellationToken.None);
        resFatal.IsResolved.Should().BeFalse();
        resFatal.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
    }

    [Fact]
    public async Task RefreshAndRetryConflictResolver_ShouldReject_WhenRefreshFailsOrEntityIdEmpty()
    {
        var resolver = new RefreshAndRetryConflictResolver<ResolverAccount>(
            refreshDelegate: (id, ct) => ValueTask.FromResult<ResolverAccount?>(null));

        var proposed = new ResolverAccount { Id = "c1", Balance = 50 };
        var conflict = ConcurrencyConflict.VersionMismatch("c1", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));

        ConflictResolution<ResolverAccount> resolution = await resolver.ResolveAsync(proposed, null, conflict);
        resolution.IsResolved.Should().BeFalse();
        resolution.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
        resolution.Reason.Should().Be("Failed to refresh entity 'c1' from storage.");

        var conflictEmptyId = ConcurrencyConflict.VersionMismatch("", "ResolverAccount", ExpectedVersion.Specific(1), ActualVersion.From(2));
        ConflictResolution<ResolverAccount> resEmpty = await resolver.ResolveAsync(proposed, null, conflictEmptyId);
        resEmpty.IsResolved.Should().BeFalse();
        resEmpty.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
    }

    [Fact]
    public async Task RefreshAndRetryConflictResolver_NullGuardsAndCancellation()
    {
        var resolver = new RefreshAndRetryConflictResolver<ResolverAccount>(
            refreshDelegate: (id, ct) => ValueTask.FromResult<ResolverAccount?>(null));

        var proposed = new ResolverAccount { Id = "c1" };
        var conflict = ConcurrencyConflict.Deleted("c1", "ResolverAccount");

        var actNullProposed = async () => await resolver.ResolveAsync(null!, null, conflict);
        await actNullProposed.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("proposedEntity");

        var actNullConflict = async () => await resolver.ResolveAsync(proposed, null, null!);
        await actNullConflict.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("conflict");

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var actCancelled = async () => await resolver.ResolveAsync(proposed, null, conflict, cts.Token);
        await actCancelled.Should().ThrowAsync<OperationCanceledException>();
    }
}
