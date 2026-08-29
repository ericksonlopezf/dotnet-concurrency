// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Resolvers;

/// <summary>
/// Resolves optimistic concurrency conflicts by executing a custom reconciliation delegate.
/// </summary>
/// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
public sealed class DelegateConflictResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    private readonly Func<TEntity, TEntity?, ConcurrencyConflict, CancellationToken, ValueTask<ConflictResolution<TEntity>>> _resolveDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateConflictResolver{TEntity}"/> class with the specified reconciliation delegate.
    /// </summary>
    /// <param name="resolveDelegate">The custom asynchronous delegate used to reconcile conflicting entity states.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resolveDelegate"/> is <see langword="null"/></exception>
    public DelegateConflictResolver(Func<TEntity, TEntity?, ConcurrencyConflict, CancellationToken, ValueTask<ConflictResolution<TEntity>>> resolveDelegate)
    {
        _resolveDelegate = resolveDelegate ?? throw new ArgumentNullException(nameof(resolveDelegate));
    }

    /// <inheritdoc />
    public async ValueTask<ConflictResolution<TEntity>> ResolveAsync(
        TEntity proposedEntity,
        TEntity? currentDatabaseEntity,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposedEntity);
        ArgumentNullException.ThrowIfNull(conflict);

        ConflictResolution<TEntity> resolution = await _resolveDelegate(
            proposedEntity, currentDatabaseEntity, conflict, cancellationToken)
            .ConfigureAwait(false);

        if (resolution.Strategy is ConflictResolutionStrategy.MergeDomainSpecific
            or ConflictResolutionStrategy.RefreshAndRetry
            or ConflictResolutionStrategy.LastWriteWinsExplicit)
        {
            ConcurrencyDiagnostics.RecordMerge(
                Activity.Current,
                typeof(TEntity).Name,
                resolution.Strategy.ToString());
        }

        return resolution;
    }
}
