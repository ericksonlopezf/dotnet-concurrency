// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Diagnostics;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class OptimisticConcurrencyCheckerTests
{
    private readonly OptimisticConcurrencyChecker _checker = OptimisticConcurrencyChecker.Instance;

    [Fact]
    public void CheckVersion_WhenMatching_ShouldReturnTrueAndNullConflict()
    {
        var expected = ExpectedVersion.Specific(5);
        var actual = new ConcurrencyVersion(5);

        bool isValid = _checker.CheckVersion(expected, actual, "item_1", "InventoryItem", out ConcurrencyConflict? conflict);

        isValid.Should().BeTrue();
        conflict.Should().BeNull();
    }

    [Fact]
    public void CheckVersion_WhenMismatch_ShouldReturnFalseAndPopulatedConflictAndRecordMetric()
    {
        long conflictCount = 0;
        string? recordedType = null;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.conflicts")
            {
                var dict = new System.Collections.Generic.Dictionary<string, string?>();
                foreach (var tag in tags)
                {
                    dict[tag.Key] = tag.Value?.ToString();
                }
                if (dict.TryGetValue("concurrency.entity_type", out var entityType) && entityType == "InventoryItem")
                {
                    conflictCount += measurement;
                    recordedType = dict.TryGetValue("concurrency.conflict_type", out var ct) ? ct : null;
                }
            }
        });
        meterListener.Start();

        var expected = ExpectedVersion.Specific(5);
        var actual = new ConcurrencyVersion(6);

        bool isValid = _checker.CheckVersion(expected, actual, "item_1", "InventoryItem", out ConcurrencyConflict? conflict);

        isValid.Should().BeFalse();
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);
        conflict.EntityId.Should().Be("item_1");
        conflict.ExpectedVersion.Should().Be(expected);
        conflict.ActualVersion.Should().Be(ActualVersion.From(actual));
        conflictCount.Should().BeGreaterThan(0);
        recordedType.Should().Be("VersionMismatch");
    }

    [Fact]
    public void CheckToken_WhenMatching_ShouldReturnTrue()
    {
        var expected = new ConcurrencyToken("token-abc", "Opaque");
        var actual = new ConcurrencyToken("token-abc", "Opaque");

        bool isValid = _checker.CheckToken(expected, actual, "doc_10", "Document", out ConcurrencyConflict? conflict);

        isValid.Should().BeTrue();
        conflict.Should().BeNull();
    }

    [Fact]
    public void CheckToken_WhenMismatch_ShouldReturnFalseAndPopulatedConflictAndRecordMetric()
    {
        long conflictCount = 0;
        string? recordedType = null;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.conflicts")
            {
                var dict = new System.Collections.Generic.Dictionary<string, string?>();
                foreach (var tag in tags)
                {
                    dict[tag.Key] = tag.Value?.ToString();
                }
                if (dict.TryGetValue("concurrency.entity_type", out var entityType) && entityType == "Document")
                {
                    conflictCount += measurement;
                    recordedType = dict.TryGetValue("concurrency.conflict_type", out var ct) ? ct : null;
                }
            }
        });
        meterListener.Start();

        var expected = new ConcurrencyToken("token-abc", "Opaque");
        var actual = new ConcurrencyToken("token-xyz", "Opaque");

        bool isValid = _checker.CheckToken(expected, actual, "doc_10", "Document", out ConcurrencyConflict? conflict);

        isValid.Should().BeFalse();
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.TokenMismatch);
        conflict.ExpectedToken.Should().Be(expected);
        conflict.ActualToken.Should().Be(actual);
        conflictCount.Should().BeGreaterThan(0);
        recordedType.Should().Be("TokenMismatch");
    }

    [Fact]
    public void CheckToken_WhenExpectedNull_ShouldReturnFalseWithNoneFallback()
    {
        var actual = new ConcurrencyToken("token-xyz", "Opaque");

        bool isValid = _checker.CheckToken(null!, actual, "doc_10", "Document", out ConcurrencyConflict? conflict);

        isValid.Should().BeFalse();
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.TokenMismatch);
        conflict.ExpectedToken.Should().Be(ConcurrencyToken.None);
        conflict.ActualToken.Should().Be(actual);
    }
}
