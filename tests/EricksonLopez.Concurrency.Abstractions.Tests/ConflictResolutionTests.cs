// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ConflictResolutionTests
{
    private sealed class Order
    {
        public string Id { get; init; } = string.Empty;
    }

    [Fact]
    public void Rejected_WithAndWithoutReason_ShouldCreateRejectedOutcome()
    {
        var resDefault = ConflictResolution.Rejected<Order>();
        resDefault.IsResolved.Should().BeFalse();
        resDefault.ResolvedEntity.Should().BeNull();
        resDefault.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
        resDefault.Reason.Should().Be("Conflict rejected by policy.");

        var resCustom = ConflictResolution.Rejected<Order>("Policy forbids overwrite.");
        resCustom.IsResolved.Should().BeFalse();
        resCustom.ResolvedEntity.Should().BeNull();
        resCustom.Strategy.Should().Be(ConflictResolutionStrategy.Reject);
        resCustom.Reason.Should().Be("Policy forbids overwrite.");
    }

    [Fact]
    public void Merged_WithAndWithoutReason_ShouldCreateMergedOutcome()
    {
        var order = new Order { Id = "ord-1" };

        var resDefault = ConflictResolution.Merged(order);
        resDefault.IsResolved.Should().BeTrue();
        resDefault.ResolvedEntity.Should().BeSameAs(order);
        resDefault.Strategy.Should().Be(ConflictResolutionStrategy.MergeDomainSpecific);
        resDefault.Reason.Should().Be("Domain-specific merge completed.");

        var resCustom = ConflictResolution.Merged(order, "Items merged successfully.");
        resCustom.IsResolved.Should().BeTrue();
        resCustom.ResolvedEntity.Should().BeSameAs(order);
        resCustom.Strategy.Should().Be(ConflictResolutionStrategy.MergeDomainSpecific);
        resCustom.Reason.Should().Be("Items merged successfully.");
    }

    [Fact]
    public void LastWriteWins_WithAndWithoutReason_ShouldCreateLastWriteWinsOutcome()
    {
        var order = new Order { Id = "ord-2" };

        var resDefault = ConflictResolution.LastWriteWins(order);
        resDefault.IsResolved.Should().BeTrue();
        resDefault.ResolvedEntity.Should().BeSameAs(order);
        resDefault.Strategy.Should().Be(ConflictResolutionStrategy.LastWriteWinsExplicit);
        resDefault.Reason.Should().Be("Explicit Last-Write-Wins applied.");

        var resCustom = ConflictResolution.LastWriteWins(order, "Overwritten by admin.");
        resCustom.IsResolved.Should().BeTrue();
        resCustom.ResolvedEntity.Should().BeSameAs(order);
        resCustom.Strategy.Should().Be(ConflictResolutionStrategy.LastWriteWinsExplicit);
        resCustom.Reason.Should().Be("Overwritten by admin.");
    }

    [Fact]
    public void RefreshedAndRetried_WithAndWithoutReason_ShouldCreateRefreshedOutcome()
    {
        var order = new Order { Id = "ord-3" };

        var resDefault = ConflictResolution.RefreshedAndRetried(order);
        resDefault.IsResolved.Should().BeTrue();
        resDefault.ResolvedEntity.Should().BeSameAs(order);
        resDefault.Strategy.Should().Be(ConflictResolutionStrategy.RefreshAndRetry);
        resDefault.Reason.Should().Be("State refreshed from persistent storage and retry succeeded.");

        var resCustom = ConflictResolution.RefreshedAndRetried(order, "Retried after reload.");
        resCustom.IsResolved.Should().BeTrue();
        resCustom.ResolvedEntity.Should().BeSameAs(order);
        resCustom.Strategy.Should().Be(ConflictResolutionStrategy.RefreshAndRetry);
        resCustom.Reason.Should().Be("Retried after reload.");
    }

    [Fact]
    public void ConflictResolution_CustomStructConstructor_EdgeCases()
    {
        var order = new Order { Id = "ord-4" };

        // ResolvedEntity is null even though Strategy is Merge => IsResolved should be false
        var nullEntityResolution = new ConflictResolution<Order>(null, ConflictResolutionStrategy.MergeDomainSpecific, "No entity");
        nullEntityResolution.IsResolved.Should().BeFalse();
        nullEntityResolution.ResolvedEntity.Should().BeNull();

        // ResolvedEntity is non-null but Strategy is Reject => IsResolved should be false
        var rejectWithEntity = new ConflictResolution<Order>(order, ConflictResolutionStrategy.Reject, "Rejected");
        rejectWithEntity.IsResolved.Should().BeFalse();
        rejectWithEntity.ResolvedEntity.Should().BeSameAs(order);
    }

    [Fact]
    public void ConflictResolutionStrategy_EnumValues()
    {
        ((byte)ConflictResolutionStrategy.Reject).Should().Be(0);
        ((byte)ConflictResolutionStrategy.LastWriteWinsExplicit).Should().Be(1);
        ((byte)ConflictResolutionStrategy.MergeDomainSpecific).Should().Be(2);
        ((byte)ConflictResolutionStrategy.RefreshAndRetry).Should().Be(3);
    }
}
