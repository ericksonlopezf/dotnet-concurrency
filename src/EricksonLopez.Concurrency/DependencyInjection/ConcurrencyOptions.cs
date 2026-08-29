// Copyright © Erickson Lopez. MIT License.
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
}
