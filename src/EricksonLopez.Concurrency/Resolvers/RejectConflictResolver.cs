// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Resolvers;

/// <summary>
/// Resolves optimistic concurrency conflicts by strictly rejecting all conflicts.
/// </summary>
/// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
public sealed class RejectConflictResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Gets the shared singleton instance of <see cref="RejectConflictResolver{TEntity}"/>.
    /// </summary>
    public static readonly RejectConflictResolver<TEntity> Instance = new();

    /// <inheritdoc />
    public ValueTask<ConflictResolution<TEntity>> ResolveAsync(
        TEntity proposedEntity,
        TEntity? currentDatabaseEntity,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(ConflictResolution.Rejected<TEntity>(conflict?.Message));
    }
}
