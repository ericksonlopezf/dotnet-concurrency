// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents the outcome of a domain or infrastructure conflict resolution evaluation.
/// </summary>
/// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
public readonly record struct ConflictResolution<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Gets the resolved entity state, or <see langword="null"/> if the conflict was rejected.
    /// </summary>
    public TEntity? ResolvedEntity { get; }

    /// <summary>
    /// Gets the conflict resolution strategy applied.
    /// </summary>
    public ConflictResolutionStrategy Strategy { get; }

    /// <summary>
    /// Gets a value indicating whether the conflict was successfully resolved with a valid entity state.
    /// </summary>
    public bool IsResolved => ResolvedEntity is not null && Strategy != ConflictResolutionStrategy.Reject;

    /// <summary>
    /// Gets an optional explanation for the chosen conflict resolution.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictResolution{TEntity}"/> struct.
    /// </summary>
    /// <param name="resolvedEntity">The resolved entity instance when resolved; otherwise, <see langword="null"/>.</param>
    /// <param name="strategy">The conflict resolution strategy applied.</param>
    /// <param name="reason">An optional explanation describing the resolution.</param>
    public ConflictResolution(TEntity? resolvedEntity, ConflictResolutionStrategy strategy, string? reason)
    {
        ResolvedEntity = resolvedEntity;
        Strategy = strategy;
        Reason = reason;
    }
}
