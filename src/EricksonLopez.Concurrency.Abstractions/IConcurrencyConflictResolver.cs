// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for domain-specific or application-level optimistic concurrency conflict resolvers.
/// </summary>
/// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
public interface IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Attempts to resolve a detected concurrency conflict between the proposed local entity state and the latest persistent state.
    /// </summary>
    /// <param name="proposedEntity">The local modified entity state that provoked the conflict.</param>
    /// <param name="currentDatabaseEntity">The current persistent entity state retrieved from storage, or <see langword="null"/> if deleted or missing.</param>
    /// <param name="conflict">The conflict descriptor containing diagnostic and contextual metadata.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="ConflictResolution{TEntity}"/> outcome.</returns>
    ValueTask<ConflictResolution<TEntity>> ResolveAsync(
        TEntity proposedEntity,
        TEntity? currentDatabaseEntity,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default);
}
