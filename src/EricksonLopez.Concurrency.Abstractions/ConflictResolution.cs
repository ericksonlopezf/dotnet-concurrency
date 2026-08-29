// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Provides non-generic helper factory methods for creating <see cref="ConflictResolution{TEntity}"/> instances.
/// </summary>
public static class ConflictResolution
{
    /// <summary>
    /// Creates a resolution indicating that the conflict was rejected and should result in an unhandled failure.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
    /// <param name="reason">An optional explanation for rejecting the conflict.</param>
    /// <returns>A rejected <see cref="ConflictResolution{TEntity}"/> instance.</returns>
    public static ConflictResolution<TEntity> Rejected<TEntity>(string? reason = null)
        where TEntity : class =>
        new(null, ConflictResolutionStrategy.Reject, reason ?? "Conflict rejected by policy.");

    /// <summary>
    /// Creates a resolution utilizing a domain-merged entity state.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
    /// <param name="mergedEntity">The reconciled entity state adhering to business invariants.</param>
    /// <param name="reason">An optional explanation describing the domain merge resolution.</param>
    /// <returns>A merged <see cref="ConflictResolution{TEntity}"/> instance.</returns>
    public static ConflictResolution<TEntity> Merged<TEntity>(TEntity mergedEntity, string? reason = null)
        where TEntity : class =>
        new(mergedEntity, ConflictResolutionStrategy.MergeDomainSpecific, reason ?? "Domain-specific merge completed.");

    /// <summary>
    /// Creates a resolution explicitly applying a Last-Write-Wins overwrite strategy.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
    /// <param name="entity">The entity state to overwrite storage with.</param>
    /// <param name="reason">An optional explanation detailing the Last-Write-Wins justification.</param>
    /// <returns>An overwrite <see cref="ConflictResolution{TEntity}"/> instance.</returns>
    public static ConflictResolution<TEntity> LastWriteWins<TEntity>(TEntity entity, string? reason = null)
        where TEntity : class =>
        new(entity, ConflictResolutionStrategy.LastWriteWinsExplicit, reason ?? "Explicit Last-Write-Wins applied.");

    /// <summary>
    /// Creates a resolution indicating that the entity was refreshed from persistent storage and re-evaluated.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
    /// <param name="refreshedEntity">The refreshed and updated entity state.</param>
    /// <param name="reason">An optional explanation describing the refresh and retry outcome.</param>
    /// <returns>A refreshed <see cref="ConflictResolution{TEntity}"/> instance.</returns>
    public static ConflictResolution<TEntity> RefreshedAndRetried<TEntity>(TEntity refreshedEntity, string? reason = null)
        where TEntity : class =>
        new(refreshedEntity, ConflictResolutionStrategy.RefreshAndRetry, reason ?? "State refreshed from persistent storage and retry succeeded.");
}
