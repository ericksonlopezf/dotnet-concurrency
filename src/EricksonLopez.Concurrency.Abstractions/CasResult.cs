// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Provides non-generic factory methods for constructing <see cref="CasResult{TEntity}"/> instances.
/// </summary>
public static class CasResult
{
    /// <summary>
    /// Creates a successful Compare-And-Swap result with the mutated entity and its new concurrency version.
    /// </summary>
    /// <typeparam name="TEntity">The type of the target entity or aggregate root.</typeparam>
    /// <param name="entity">The mutated entity instance.</param>
    /// <param name="newVersion">The new concurrency version assigned after mutation.</param>
    /// <returns>A successful <see cref="CasResult{TEntity}"/> containing the mutated entity and its assigned version.</returns>
    public static CasResult<TEntity> Succeeded<TEntity>(TEntity entity, ConcurrencyVersion newVersion)
        where TEntity : class =>
        new(entity, newVersion, null);

    /// <summary>
    /// Creates a conflicted Compare-And-Swap result encapsulating the conflict descriptor.
    /// </summary>
    /// <typeparam name="TEntity">The type of the target entity or aggregate root.</typeparam>
    /// <param name="conflict">The conflict descriptor detailing the cause of failure.</param>
    /// <returns>A conflicted <see cref="CasResult{TEntity}"/> containing the conflict descriptor.</returns>
    public static CasResult<TEntity> Conflicted<TEntity>(ConcurrencyConflict conflict)
        where TEntity : class =>
        new(null, null, conflict);
}
