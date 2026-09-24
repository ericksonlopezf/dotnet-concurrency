// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.DependencyInjection;

/// <summary>
/// Specifies configuration options and default behaviors for the concurrency framework.
/// </summary>
public sealed class ConcurrencyOptions
{
    /// <summary>
    /// Gets or sets the default strategy applied when a concurrency conflict is detected.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="ConflictResolutionStrategy.Reject"/>.
    /// </remarks>
    public ConflictResolutionStrategy DefaultResolutionStrategy { get; set; } = ConflictResolutionStrategy.Reject;

    /// <summary>
    /// Gets or sets a value indicating whether OpenTelemetry activity tracking and metrics are enabled.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="true"/>.
    /// </remarks>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Gets or sets the default conflict classification assigned when generating generic version mismatch conflicts.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="ConcurrencyConflictClassification.Transient"/>.
    /// </remarks>
    public ConcurrencyConflictClassification DefaultConflictClassification { get; set; } = ConcurrencyConflictClassification.Transient;

    /// <summary>
    /// Gets or sets a value indicating whether detailed contextual tags are attached to OpenTelemetry activities.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="true"/>.
    /// </remarks>
    public bool RecordDetailedActivityTags { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether unresolved conflicts throw a <see cref="ConcurrencyException"/> automatically.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false"/> (returns conflict model for caller inspection).
    /// </remarks>
    public bool ThrowOnUnresolvedConflict { get; set; }

    /// <summary>
    /// Gets or sets the default maximum time to wait when acquiring an optimistic in-memory lock.
    /// </summary>
    /// <remarks>
    /// The default value is 10 seconds. If a lock cannot be acquired within this time, a <see cref="TimeoutException"/> is thrown to prevent deadlocks.
    /// </remarks>
    public TimeSpan DefaultMaxAcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the default maximum time a mutation delegate is allowed to execute while holding the in-memory lock.
    /// </summary>
    /// <remarks>
    /// The default value is 30 seconds. If the delegate execution exceeds this time, the provided cancellation token will be cancelled to prevent thread-pool starvation.
    /// </remarks>
    public TimeSpan DefaultMaxExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Calculates the default stripe count based on the specified processor count.
    /// </summary>
    /// <param name="processorCount">The number of available processors.</param>
    /// <returns>The calculated stripe count, with a minimum of 256.</returns>
    public static int CalculateDefaultStripeCount(int processorCount) =>
        Math.Max(256, processorCount * 8);

    /// <summary>
    /// Gets or sets the number of stripes (partitions) used by the concurrency controller to shard internal locks.
    /// </summary>
    /// <remarks>
    /// The default value is calculated as <see cref="Environment.ProcessorCount"/> * 8, with a minimum of 256. 
    /// Higher values reduce false-sharing contention in highly concurrent systems at the cost of slight memory overhead.
    /// </remarks>
    public int StripeCount { get; set; } = CalculateDefaultStripeCount(Environment.ProcessorCount);
}
