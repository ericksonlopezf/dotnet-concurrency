// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Diagnostics;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class ConcurrencyDiagnosticsTests
{
    [Fact]
    public void Diagnostics_MetadataAndInstruments_ShouldBeConfiguredCorrectly()
    {
        ConcurrencyDiagnostics.SourceName.Should().Be("EricksonLopez.Concurrency");
        ConcurrencyDiagnostics.Version.Should().Be("1.0.0");
        ConcurrencyDiagnostics.ActivitySource.Name.Should().Be("EricksonLopez.Concurrency");
        ConcurrencyDiagnostics.ActivitySource.Version.Should().Be("1.0.0");

        ConcurrencyDiagnostics.Meter.Name.Should().Be("EricksonLopez.Concurrency");
        ConcurrencyDiagnostics.Meter.Version.Should().Be("1.0.0");

        ConcurrencyDiagnostics.ConflictsCounter.Name.Should().Be("concurrency.conflicts");
        ConcurrencyDiagnostics.ConflictsCounter.Unit.Should().Be("{conflict}");
        ConcurrencyDiagnostics.ConflictsCounter.Description.Should().Be("Measures the number of optimistic concurrency conflicts detected.");

        ConcurrencyDiagnostics.SuccessesCounter.Name.Should().Be("concurrency.successes");
        ConcurrencyDiagnostics.SuccessesCounter.Unit.Should().Be("{success}");
        ConcurrencyDiagnostics.SuccessesCounter.Description.Should().Be("Measures the number of successful concurrency operations.");

        ConcurrencyDiagnostics.FailuresCounter.Name.Should().Be("concurrency.failures");
        ConcurrencyDiagnostics.FailuresCounter.Unit.Should().Be("{failure}");
        ConcurrencyDiagnostics.FailuresCounter.Description.Should().Be("Measures the number of failed concurrency operations.");

        ConcurrencyDiagnostics.MergesCounter.Name.Should().Be("concurrency.merges");
        ConcurrencyDiagnostics.MergesCounter.Unit.Should().Be("{merge}");
        ConcurrencyDiagnostics.MergesCounter.Description.Should().Be("Measures the number of domain conflict merges executed.");

        ConcurrencyDiagnostics.OperationDurationHistogram.Name.Should().Be("concurrency.duration");
        ConcurrencyDiagnostics.OperationDurationHistogram.Unit.Should().Be("ms");
        ConcurrencyDiagnostics.OperationDurationHistogram.Description.Should().Be("Measures the duration in milliseconds of concurrency verification and operations.");
    }

    [Fact]
    public void Diagnostics_WithActiveActivityListener_ShouldPopulateTagsAndStatus()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using (Activity? activity = ConcurrencyDiagnostics.StartActivity("concurrency.test_op", "Customer", "cust_100"))
        {
            activity.Should().NotBeNull();
            activity!.GetTagItem("concurrency.entity_type").Should().Be("Customer");
            activity.GetTagItem("concurrency.entity_id").Should().Be("cust_100");
            activity.GetTagItem("concurrency.operation").Should().Be("concurrency.test_op");

            ConcurrencyDiagnostics.RecordSuccess(activity, "Customer");
            activity.Status.Should().Be(ActivityStatusCode.Ok);
            activity.GetTagItem("concurrency.conflict").Should().Be(false);

            ConcurrencyDiagnostics.RecordConflict(activity, "VersionMismatch", "Customer");
            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("Concurrency conflict: VersionMismatch");
            activity.GetTagItem("concurrency.conflict").Should().Be(true);
            activity.GetTagItem("concurrency.conflict_type").Should().Be("VersionMismatch");

            ConcurrencyDiagnostics.RecordMerge(activity, "Customer", "MergeDomainSpecific");
            activity.GetTagItem("concurrency.merged").Should().Be(true);
            activity.GetTagItem("concurrency.strategy").Should().Be("MergeDomainSpecific");
        }

        // Test RecordMerge with null strategy and active activity
        using (Activity? activity2 = ConcurrencyDiagnostics.StartActivity("concurrency.test_merge_default", "Order", "ord_50"))
        {
            activity2.Should().NotBeNull();
            ConcurrencyDiagnostics.RecordMerge(activity2, "Order", null);
            activity2!.GetTagItem("concurrency.merged").Should().Be(true);
            activity2.GetTagItem("concurrency.strategy").Should().BeNull();
        }
    }

    [Fact]
    public void Diagnostics_WithMeterListener_ShouldRecordMetricMeasurements()
    {
        long conflictCount = 0;
        string? recordedConflictType = null;
        string? recordedConflictEntity = null;

        long successCount = 0;
        string? recordedSuccessEntity = null;

        long mergeCount = 0;
        string? recordedMergeEntity = null;
        string? recordedMergeStrategy = null;

        double recordedDuration = 0;

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ConcurrencyDiagnostics.SourceName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? entity = null;
            string? conflictType = null;
            string? strategy = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "concurrency.entity_type") entity = tag.Value?.ToString();
                if (tag.Key == "concurrency.conflict_type") conflictType = tag.Value?.ToString();
                if (tag.Key == "concurrency.strategy") strategy = tag.Value?.ToString();
            }

            if (entity != "Invoice_Diagnostics_Unique") return;

            if (instrument.Name == "concurrency.conflicts")
            {
                conflictCount += measurement;
                recordedConflictType = conflictType;
                recordedConflictEntity = entity;
            }
            else if (instrument.Name == "concurrency.successes")
            {
                successCount += measurement;
                recordedSuccessEntity = entity;
            }
            else if (instrument.Name == "concurrency.merges")
            {
                mergeCount += measurement;
                recordedMergeEntity = entity;
                recordedMergeStrategy = strategy;
            }
        });

        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "concurrency.duration")
            {
                recordedDuration = measurement;
            }
        });

        meterListener.Start();

        ConcurrencyDiagnostics.RecordConflict(null, "VersionMismatch", "Invoice_Diagnostics_Unique");
        conflictCount.Should().Be(1);
        recordedConflictType.Should().Be("VersionMismatch");
        recordedConflictEntity.Should().Be("Invoice_Diagnostics_Unique");

        ConcurrencyDiagnostics.RecordSuccess(null, "Invoice_Diagnostics_Unique");
        successCount.Should().Be(1);
        recordedSuccessEntity.Should().Be("Invoice_Diagnostics_Unique");

        ConcurrencyDiagnostics.RecordMerge(null, "Invoice_Diagnostics_Unique", "CustomMerge");
        mergeCount.Should().Be(1);
        recordedMergeEntity.Should().Be("Invoice_Diagnostics_Unique");
        recordedMergeStrategy.Should().Be("CustomMerge");

        ConcurrencyDiagnostics.RecordMerge(null, "Invoice_Diagnostics_Unique", null);
        mergeCount.Should().Be(2);
        recordedMergeStrategy.Should().Be("MergeDomainSpecific");

        ConcurrencyDiagnostics.OperationDurationHistogram.Record(45.5);
        recordedDuration.Should().Be(45.5);
    }

    [Fact]
    public void RecordConflictAndSuccess_ShouldNotThrow_WhenNoListenerActive()
    {
        ConcurrencyDiagnostics.RecordConflict(null, "VersionMismatch", "Customer");
        ConcurrencyDiagnostics.RecordSuccess(null, "Customer");
        ConcurrencyDiagnostics.RecordMerge(null, "Customer", "MergeDomainSpecific");
        ConcurrencyDiagnostics.RecordMerge(null, "Customer", null);
        ConcurrencyDiagnostics.OperationDurationHistogram.Record(12.5);
    }
}
