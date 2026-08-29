// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class CasResultTests
{
    private sealed class SampleEntity
    {
        public string Id { get; init; } = string.Empty;
    }

    [Fact]
    public void CasResult_Succeeded_ShouldPopulateEntityAndVersion()
    {
        var entity = new SampleEntity { Id = "e1" };
        var version = new ConcurrencyVersion(5);

        CasResult<SampleEntity> result = CasResult.Succeeded(entity, version);

        result.IsSuccess.Should().BeTrue();
        result.IsConflict.Should().BeFalse();
        result.Entity.Should().BeSameAs(entity);
        result.NewVersion.Should().Be(version);
        result.Conflict.Should().BeNull();
    }

    [Fact]
    public void CasResult_Conflicted_ShouldPopulateConflictDescriptor()
    {
        var conflict = ConcurrencyConflict.Deleted("e1", nameof(SampleEntity));

        CasResult<SampleEntity> result = CasResult.Conflicted<SampleEntity>(conflict);

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeTrue();
        result.Entity.Should().BeNull();
        result.NewVersion.Should().BeNull();
        result.Conflict.Should().BeSameAs(conflict);
    }

    [Fact]
    public void CasResult_CustomStructConstructor_ShouldExposeConfiguredProperties()
    {
        var entity = new SampleEntity { Id = "custom" };
        var version = new ConcurrencyVersion(12);
        var conflict = ConcurrencyConflict.Deleted("custom", nameof(SampleEntity));

        // Both populated (edge case)
        var mixed = new CasResult<SampleEntity>(entity, version, conflict);
        mixed.IsSuccess.Should().BeFalse();
        mixed.IsConflict.Should().BeTrue();
        mixed.Entity.Should().BeSameAs(entity);
        mixed.NewVersion.Should().Be(version);
        mixed.Conflict.Should().BeSameAs(conflict);

        // Neither populated
        var empty = new CasResult<SampleEntity>(null, null, null);
        empty.IsSuccess.Should().BeFalse();
        empty.IsConflict.Should().BeFalse();
        empty.Entity.Should().BeNull();
        empty.NewVersion.Should().BeNull();
        empty.Conflict.Should().BeNull();
    }
}
