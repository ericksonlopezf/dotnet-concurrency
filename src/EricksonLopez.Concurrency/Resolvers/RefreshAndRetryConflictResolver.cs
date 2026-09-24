// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Resolvers;

/// <summary>
/// Resolves optimistic concurrency conflicts by reloading the latest state from persistent storage and re-applying mutations.
/// </summary>
/// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
public sealed class RefreshAndRetryConflictResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    private readonly Func<string, CancellationToken, ValueTask<TEntity?>> _refreshDelegate;
    private readonly Func<TEntity, TEntity, TEntity>? _reapplyDelegate;
    private readonly int _maxRetries;
    private readonly Func<int, TimeSpan>? _retryDelayProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshAndRetryConflictResolver{TEntity}"/> class with the specified refresh delegate.
    /// </summary>
    /// <param name="refreshDelegate">An asynchronous delegate that retrieves the latest persistent state for a given entity identifier.</param>
    /// <param name="maxRetries">The maximum number of reload attempts permitted. Must be at least 1.</param>
    /// <param name="retryDelayProvider">An optional delegate supplying a backoff delay based on the current retry attempt (1-based).</param>
    /// <exception cref="ArgumentNullException"><paramref name="refreshDelegate"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is less than 1</exception>
    public RefreshAndRetryConflictResolver(
        Func<string, CancellationToken, ValueTask<TEntity?>> refreshDelegate,
        int maxRetries = 3,
        Func<int, TimeSpan>? retryDelayProvider = null)
    {
        ArgumentNullException.ThrowIfNull(refreshDelegate);
        if (maxRetries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Maximum retries must be at least 1.");
        }

        _refreshDelegate = refreshDelegate;
        _reapplyDelegate = null;
        _maxRetries = maxRetries;
        _retryDelayProvider = retryDelayProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshAndRetryConflictResolver{TEntity}"/> class with refresh and reapply delegates.
    /// </summary>
    /// <param name="refreshDelegate">An asynchronous delegate that retrieves the latest persistent state for a given entity identifier.</param>
    /// <param name="reapplyDelegate">A delegate that re-applies local changes onto the freshly reloaded state.</param>
    /// <param name="maxRetries">The maximum number of reload attempts permitted. Must be at least 1.</param>
    /// <param name="retryDelayProvider">An optional delegate supplying a backoff delay based on the current retry attempt (1-based).</param>
    /// <exception cref="ArgumentNullException"><paramref name="refreshDelegate"/> or <paramref name="reapplyDelegate"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is less than 1</exception>
    public RefreshAndRetryConflictResolver(
        Func<string, CancellationToken, ValueTask<TEntity?>> refreshDelegate,
        Func<TEntity, TEntity, TEntity> reapplyDelegate,
        int maxRetries = 3,
        Func<int, TimeSpan>? retryDelayProvider = null)
    {
        ArgumentNullException.ThrowIfNull(refreshDelegate);
        ArgumentNullException.ThrowIfNull(reapplyDelegate);
        if (maxRetries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Maximum retries must be at least 1.");
        }

        _refreshDelegate = refreshDelegate;
        _reapplyDelegate = reapplyDelegate;
        _maxRetries = maxRetries;
        _retryDelayProvider = retryDelayProvider;
    }

    /// <summary>
    /// Gets the maximum number of retry reload attempts permitted.
    /// </summary>
    public int MaxRetries => _maxRetries;

    /// <summary>
    /// Gets the optional retry delay/backoff provider function.
    /// </summary>
    public Func<int, TimeSpan>? RetryDelayProvider => _retryDelayProvider;

    /// <inheritdoc />
    public async ValueTask<ConflictResolution<TEntity>> ResolveAsync(
        TEntity proposedEntity,
        TEntity? currentDatabaseEntity,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposedEntity);
        ArgumentNullException.ThrowIfNull(conflict);

        cancellationToken.ThrowIfCancellationRequested();

        // If the conflict classification is non-retryable or fatal, reject immediately
        if (conflict.Classification is ConcurrencyConflictClassification.NonRetryable
            or ConcurrencyConflictClassification.Fatal)
        {
            return ConflictResolution.Rejected<TEntity>($"Conflict '{conflict.ConflictType}' is classified as {conflict.Classification} and cannot be refreshed.");
        }

        TEntity? latestState = currentDatabaseEntity;
        if (latestState is null && !string.IsNullOrEmpty(conflict.EntityId))
        {
            latestState = await RefreshLatestStateAsync(conflict.EntityId, cancellationToken).ConfigureAwait(false);
        }

        if (latestState is null)
        {
            return ConflictResolution.Rejected<TEntity>($"Failed to refresh entity '{conflict.EntityId}' from storage.");
        }

        TEntity resolvedState;
        if (_reapplyDelegate is not null)
        {
            resolvedState = _reapplyDelegate(proposedEntity, latestState);
        }
        else
        {
            resolvedState = latestState;
        }

        ConcurrencyDiagnostics.RecordMerge(
            Activity.Current,
            typeof(TEntity).Name,
            nameof(ConflictResolutionStrategy.RefreshAndRetry));

        return ConflictResolution.RefreshedAndRetried(resolvedState, $"State refreshed from storage and reconciled with strategy {ConflictResolutionStrategy.RefreshAndRetry}.");
    }

    internal Func<TimeSpan, CancellationToken, Task> DelayAsync { get; set; } = Task.Delay;

    private async ValueTask<TEntity?> RefreshLatestStateAsync(
        string entityId,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntity? state = await _refreshDelegate(entityId, cancellationToken).ConfigureAwait(false);
            if (state is not null)
            {
                return state;
            }

            if (attempt < _maxRetries && _retryDelayProvider is not null)
            {
                TimeSpan delay = _retryDelayProvider(attempt);
                if (delay > TimeSpan.Zero)
                {
                    await DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return null;
    }
}
