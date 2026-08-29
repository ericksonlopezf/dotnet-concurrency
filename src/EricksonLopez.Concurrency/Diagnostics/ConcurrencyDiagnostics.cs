// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EricksonLopez.Concurrency.Diagnostics;

/// <summary>
/// Provides OpenTelemetry activities, meters, counters, and metrics instrumentation for concurrency operations.
/// </summary>
public static class ConcurrencyDiagnostics
{
    /// <summary>
    /// Specifies the instrumentation name for OpenTelemetry activity and meter sources.
    /// </summary>
    public const string SourceName = "EricksonLopez.Concurrency";

    /// <summary>
    /// Specifies the current semantic version of the instrumentation source.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.ActivitySource"/> for distributed tracing instrumentation.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, Version);

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.Metrics.Meter"/> for metrics collection and telemetry.
    /// </summary>
    public static readonly Meter Meter = new(SourceName, Version);

    /// <summary>
    /// Gets the counter measuring the total number of optimistic concurrency conflicts detected.
    /// </summary>
    public static readonly Counter<long> ConflictsCounter = Meter.CreateCounter<long>(
        "concurrency.conflicts",
        unit: "{conflict}",
        description: "Measures the number of optimistic concurrency conflicts detected.");

    /// <summary>
    /// Gets the counter measuring the total number of successful concurrency operations.
    /// </summary>
    public static readonly Counter<long> SuccessesCounter = Meter.CreateCounter<long>(
        "concurrency.successes",
        unit: "{success}",
        description: "Measures the number of successful concurrency operations.");

    /// <summary>
    /// Gets the counter measuring the total number of failed concurrency operations.
    /// </summary>
    public static readonly Counter<long> FailuresCounter = Meter.CreateCounter<long>(
        "concurrency.failures",
        unit: "{failure}",
        description: "Measures the number of failed concurrency operations.");

    /// <summary>
    /// Gets the counter measuring the total number of domain conflict merges executed.
    /// </summary>
    public static readonly Counter<long> MergesCounter = Meter.CreateCounter<long>(
        "concurrency.merges",
        unit: "{merge}",
        description: "Measures the number of domain conflict merges executed.");

    /// <summary>
    /// Gets the histogram measuring the duration in milliseconds of concurrency evaluations and state mutations.
    /// </summary>
    public static readonly Histogram<double> OperationDurationHistogram = Meter.CreateHistogram<double>(
        "concurrency.duration",
        unit: "ms",
        description: "Measures the duration in milliseconds of concurrency verification and operations.");

    /// <summary>
    /// Starts a new OpenTelemetry tracing activity for a concurrency operation.
    /// </summary>
    /// <param name="operationName">The name of the concurrency operation.</param>
    /// <param name="entityType">The type name of the target entity.</param>
    /// <param name="entityId">The unique identifier of the target entity.</param>
    /// <returns>The started <see cref="Activity"/> if tracing is enabled; otherwise, <see langword="null"/>.</returns>
    public static Activity? StartActivity(string operationName, string entityType, string entityId)
    {
        Activity? activity = ActivitySource.StartActivity(operationName);
        if (activity is not null && activity.IsAllDataRequested)
        {
            activity.SetTag("concurrency.entity_type", entityType);
            activity.SetTag("concurrency.entity_id", entityId);
            activity.SetTag("concurrency.operation", operationName);
        }

        return activity;
    }

    /// <summary>
    /// Records a concurrency conflict event on the specified activity and increments metrics.
    /// </summary>
    /// <param name="activity">The active tracing activity, or <see langword="null"/>.</param>
    /// <param name="conflictType">The category or discriminator of the conflict.</param>
    /// <param name="entityType">The type name of the target entity.</param>
    public static void RecordConflict(Activity? activity, string conflictType, string entityType)
    {
        ConflictsCounter.Add(1, new KeyValuePair<string, object?>("concurrency.conflict_type", conflictType), new KeyValuePair<string, object?>("concurrency.entity_type", entityType));

        if (activity is not null && activity.IsAllDataRequested)
        {
            activity.SetStatus(ActivityStatusCode.Error, $"Concurrency conflict: {conflictType}");
            activity.SetTag("concurrency.conflict", true);
            activity.SetTag("concurrency.conflict_type", conflictType);
        }
    }

    /// <summary>
    /// Records a successful concurrency check or state mutation on the specified activity and increments metrics.
    /// </summary>
    /// <param name="activity">The active tracing activity, or <see langword="null"/>.</param>
    /// <param name="entityType">The type name of the target entity.</param>
    public static void RecordSuccess(Activity? activity, string entityType)
    {
        SuccessesCounter.Add(1, new KeyValuePair<string, object?>("concurrency.entity_type", entityType));

        if (activity is not null && activity.IsAllDataRequested)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.SetTag("concurrency.conflict", false);
        }
    }

    /// <summary>
    /// Records a domain or strategy conflict merge reconciliation and increments the merges metric counter.
    /// </summary>
    /// <param name="activity">The active tracing activity, or <see langword="null"/>.</param>
    /// <param name="entityType">The type name of the target entity.</param>
    /// <param name="strategy">The name of the resolution strategy applied.</param>
    public static void RecordMerge(Activity? activity, string entityType, string? strategy = null)
    {
        MergesCounter.Add(1,
            new KeyValuePair<string, object?>("concurrency.entity_type", entityType),
            new KeyValuePair<string, object?>("concurrency.strategy", strategy ?? "MergeDomainSpecific"));

        if (activity is not null && activity.IsAllDataRequested)
        {
            activity.SetTag("concurrency.merged", true);
            if (strategy is not null)
            {
                activity.SetTag("concurrency.strategy", strategy);
            }
        }
    }
}
