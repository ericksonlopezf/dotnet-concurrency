// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Controllers;

/// <summary>
/// Coordinates optimistic concurrency validations and in-memory Compare-And-Swap (CAS) state transitions.
/// </summary>
public sealed class ConcurrencyController : IConcurrencyController, IDisposable, IAsyncDisposable
{
    private readonly IConcurrencyChecker _checker;
    private readonly ConcurrencyOptions _options;
    private readonly object[] _stripes;
    private readonly Dictionary<string, RefCountedLock>[] _dictionaries;
    private readonly ConcurrentQueue<RefCountedLock> _lockPool = new();
    
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _isDisposed;

    private static readonly AsyncLocal<ReentrancyNode?> _activeEntityLocks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyController"/> class with default settings.
    /// </summary>
    public ConcurrencyController()
        : this(null, (ConcurrencyOptions?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyController"/> class with the specified checker.
    /// </summary>
    /// <param name="checker">The concurrency checker used to evaluate version and token constraints, or <see langword="null"/> to use <see cref="OptimisticConcurrencyChecker.Instance"/>.</param>
    public ConcurrencyController(IConcurrencyChecker? checker)
        : this(checker, (ConcurrencyOptions?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyController"/> class with the specified options.
    /// </summary>
    /// <param name="options">Configuration options governing conflict handling and diagnostics.</param>
    public ConcurrencyController(ConcurrencyOptions? options)
        : this(null, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyController"/> class.
    /// </summary>
    /// <param name="checker">The concurrency checker used to evaluate version and token constraints, or <see langword="null"/> to use <see cref="OptimisticConcurrencyChecker.Instance"/>.</param>
    /// <param name="options">Configuration options governing conflict handling and diagnostics.</param>
    public ConcurrencyController(
        IConcurrencyChecker? checker,
        ConcurrencyOptions? options)
    {
        _checker = checker ?? OptimisticConcurrencyChecker.Instance;
        _options = options ?? new ConcurrencyOptions();

        int stripeCount = Math.Max(1, _options.StripeCount);
        _stripes = new object[stripeCount];
        _dictionaries = new Dictionary<string, RefCountedLock>[stripeCount];
        for (int i = 0; i < stripeCount; i++)
        {
            _stripes[i] = new object();
            _dictionaries[i] = new Dictionary<string, RefCountedLock>(StringComparer.Ordinal);
        }
    }

    private int GetStripeIndex(string entityId)
    {
        // Use a positive modulo to determine stripe index
        int hashCode = entityId.GetHashCode(StringComparison.Ordinal);
        return (hashCode & 0x7FFFFFFF) % _stripes.Length;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="ObjectDisposedException">This controller instance has already been disposed</exception>
    /// <exception cref="ConcurrencyException">A concurrency conflict occurred and <see cref="ConcurrencyOptions.ThrowOnUnresolvedConflict"/> is <see langword="true"/></exception>
    public ConcurrencyConflict? VerifyVersion<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId)
        where TEntity : class, IVersionedEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        string entityType = typeof(TEntity).Name;
        using Activity? activity = _options.EnableDiagnostics
            ? ConcurrencyDiagnostics.StartActivity("concurrency.verify_version", entityType, entityId)
            : null;

        if (activity is not null && _options.RecordDetailedActivityTags)
        {
            activity.SetTag("concurrency.expected_version", expected.ToString());
            activity.SetTag("concurrency.actual_version", entity.Version.ToString(CultureInfo.InvariantCulture));
        }

        var actual = new ConcurrencyVersion(entity.Version);
        if (_checker.CheckVersion(expected, actual, entityId, entityType, out ConcurrencyConflict? conflict))
        {
            ConcurrencyDiagnostics.RecordSuccess(activity, entityType);
            return null;
        }

        if (activity is not null && activity.IsAllDataRequested)
        {
            activity.SetStatus(ActivityStatusCode.Error, $"Concurrency conflict: {nameof(ConcurrencyConflictType.VersionMismatch)}");
            activity.SetTag("concurrency.conflict", true);
            activity.SetTag("concurrency.conflict_type", nameof(ConcurrencyConflictType.VersionMismatch));
        }

        if (_options.ThrowOnUnresolvedConflict && conflict is not null)
        {
            throw new ConcurrencyException(conflict);
        }

        return conflict;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="expected"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="ObjectDisposedException">This controller instance has already been disposed</exception>
    /// <exception cref="ConcurrencyException">A concurrency conflict occurred and <see cref="ConcurrencyOptions.ThrowOnUnresolvedConflict"/> is <see langword="true"/></exception>
    public ConcurrencyConflict? VerifyToken<TEntity>(
        TEntity entity,
        IConcurrencyToken expected,
        string entityId)
        where TEntity : class, IConcurrencyAware
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        string entityType = typeof(TEntity).Name;
        using Activity? activity = _options.EnableDiagnostics
            ? ConcurrencyDiagnostics.StartActivity("concurrency.verify_token", entityType, entityId)
            : null;

        if (activity is not null && _options.RecordDetailedActivityTags)
        {
            activity.SetTag("concurrency.expected_token", expected.Value);
            activity.SetTag("concurrency.actual_token", entity.ConcurrencyToken?.Value);
        }

        IConcurrencyToken actual = entity.ConcurrencyToken ?? ConcurrencyToken.None;
        if (_checker.CheckToken(expected, actual, entityId, entityType, out ConcurrencyConflict? conflict))
        {
            ConcurrencyDiagnostics.RecordSuccess(activity, entityType);
            return null;
        }

        if (activity is not null && activity.IsAllDataRequested)
        {
            activity.SetStatus(ActivityStatusCode.Error, $"Concurrency conflict: {nameof(ConcurrencyConflictType.TokenMismatch)}");
            activity.SetTag("concurrency.conflict", true);
            activity.SetTag("concurrency.conflict_type", nameof(ConcurrencyConflictType.TokenMismatch));
        }

        if (_options.ThrowOnUnresolvedConflict && conflict is not null)
        {
            throw new ConcurrencyException(conflict);
        }

        return conflict;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="mutate"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="ObjectDisposedException">This controller instance has already been disposed</exception>
    /// <exception cref="InvalidOperationException">A reentrant call was detected for <paramref name="entityId"/>, or an immutable entity did not advance its version</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled</exception>
    /// <exception cref="TimeoutException">The in-memory lock could not be acquired within the configured timeout</exception>
    public async ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId,
        Func<TEntity, CancellationToken, ValueTask<TEntity>> mutate,
        CancellationToken cancellationToken = default)
        where TEntity : class, IVersionedEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(mutate);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        ObjectDisposedException.ThrowIf(_isDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (_activeEntityLocks.Value?.Contains(entityId) == true)
        {
            throw new InvalidOperationException($"Reentrant CAS invocation detected for entity '{entityId}'. Nested calls to ExecuteCasAsync for the same entity id on the same asynchronous execution context lead to deadlocks and are not permitted.");
        }

        var previousLocks = _activeEntityLocks.Value;
        _activeEntityLocks.Value = new ReentrancyNode(entityId, previousLocks);

        try
        {
            string entityType = typeof(TEntity).Name;
            using Activity? activity = _options.EnableDiagnostics
                ? ConcurrencyDiagnostics.StartActivity("concurrency.execute_cas", entityType, entityId)
                : null;

            RefCountedLock entityLock = AcquireLock(entityId);
            bool lockTaken = false;
            try
            {
                using var linkedAcquisitionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
                bool acquired = await entityLock.Semaphore.WaitAsync(_options.DefaultMaxAcquisitionTimeout, linkedAcquisitionCts.Token).ConfigureAwait(false);
                if (!acquired)
                {
                    throw new TimeoutException($"The in-memory concurrency lock for entity '{entityId}' could not be acquired within the configured timeout of {_options.DefaultMaxAcquisitionTimeout.TotalSeconds} seconds. This is often caused by a cyclic lock-ordering deadlock (AB/BA) or a long-running synchronous block in a previous mutation delegate.");
                }
                lockTaken = true;

                linkedAcquisitionCts.Token.ThrowIfCancellationRequested();

                var currentVersion = new ConcurrencyVersion(entity.Version);
                if (!_checker.CheckVersion(expected, currentVersion, entityId, entityType, out ConcurrencyConflict? conflict))
                {
                    if (activity is not null && activity.IsAllDataRequested)
                    {
                        activity.SetStatus(ActivityStatusCode.Error, $"Concurrency conflict: {nameof(ConcurrencyConflictType.VersionMismatch)}");
                        activity.SetTag("concurrency.conflict", true);
                        activity.SetTag("concurrency.conflict_type", nameof(ConcurrencyConflictType.VersionMismatch));
                    }

                    ConcurrencyConflict effectiveConflict = conflict ?? ConcurrencyConflict.VersionMismatch(
                        entityId,
                        entityType,
                        expected,
                        actual: new ActualVersion(currentVersion));

                    if (_options.ThrowOnUnresolvedConflict)
                    {
                        throw new ConcurrencyException(effectiveConflict);
                    }

                    return CasResult.Conflicted<TEntity>(effectiveConflict);
                }

                long startTimestamp = Stopwatch.GetTimestamp();
                
                // Protect against long-running mutate delegates
                using var executionTimeoutCts = new CancellationTokenSource(_options.DefaultMaxExecutionTimeout);
                using var executionLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token, executionTimeoutCts.Token);
                
                TEntity mutated;
                try
                {
                    mutated = await mutate(entity, executionLinkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (executionTimeoutCts.IsCancellationRequested)
                {
                    throw new TimeoutException($"The mutation delegate for entity '{entityId}' exceeded the maximum execution timeout of {_options.DefaultMaxExecutionTimeout.TotalSeconds} seconds.");
                }

                double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
                ConcurrencyDiagnostics.OperationDurationHistogram.Record(elapsedMs);

                ConcurrencyVersion nextVersion = currentVersion.Next();
                if (entity.Version != currentVersion.Value && entity.Version != nextVersion.Value)
                {
                    ConcurrencyDiagnostics.RecordConflict(activity, nameof(ConcurrencyConflictType.VersionMismatch), entityType);

                    ConcurrencyConflict toctouConflict = ConcurrencyConflict.VersionMismatch(
                        entityId,
                        entityType,
                        expected,
                        actual: new ActualVersion(entity.Version));

                    if (_options.ThrowOnUnresolvedConflict)
                    {
                        throw new ConcurrencyException(toctouConflict);
                    }

                    return CasResult.Conflicted<TEntity>(toctouConflict);
                }

                if (mutated is IMutableVersionedEntity mutable)
                {
                    mutable.Version = nextVersion.Value;
                }
                else if (mutated.Version != nextVersion.Value)
                {
                    throw new InvalidOperationException($"Entity of type '{entityType}' does not implement '{nameof(IMutableVersionedEntity)}' and the mutation delegate did not advance the entity version to '{nextVersion.Value}'. In-memory CAS requires version progression to prevent stale updates.");
                }

                ConcurrencyDiagnostics.RecordSuccess(activity, entityType);

                return CasResult.Succeeded(mutated, nextVersion);
            }
            finally
            {
                if (lockTaken)
                {
                    entityLock.Semaphore.Release();
                }
                ReleaseLock(entityId, entityLock);
            }
        }
        finally
        {
            _activeEntityLocks.Value = previousLocks;
        }
    }

    private RefCountedLock AcquireLock(string entityId)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        int stripeIndex = GetStripeIndex(entityId);
        object stripeLock = _stripes[stripeIndex];
        var dictionary = _dictionaries[stripeIndex];

        lock (stripeLock)
        {
            if (dictionary.TryGetValue(entityId, out RefCountedLock? existing))
            {
                existing.RefCount++;
                return existing;
            }

            if (!_lockPool.TryDequeue(out var created))
            {
                created = new RefCountedLock();
            }
            created.RefCount = 1;
            dictionary.Add(entityId, created);
            return created;
        }
    }

    private void ReleaseLock(string entityId, RefCountedLock entityLock)
    {
        int stripeIndex = GetStripeIndex(entityId);
        object stripeLock = _stripes[stripeIndex];
        var dictionary = _dictionaries[stripeIndex];

        bool shouldDispose = false;

        lock (stripeLock)
        {
            entityLock.RefCount--;
            if (entityLock.RefCount == 0)
            {
                dictionary.Remove(entityId);
                shouldDispose = true;
            }
        }

        if (shouldDispose)
        {
            if (_isDisposed)
            {
                entityLock.Dispose();
            }
            else
            {
                entityLock.Reset();
                if (_isDisposed)
                {
                    entityLock.Dispose();
                }
                else
                {
                    _lockPool.Enqueue(entityLock);
                }
            }
        }
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        
        _isDisposed = true;
        _disposeCts.Cancel();
        
        // We do not eagerly dispose active RefCountedLocks here. 
        // When active operations catch OperationCanceledException via _disposeCts,
        // they will call ReleaseLock in their finally block, which safely disposes them.
        for (int i = 0; i < _stripes.Length; i++)
        {
            lock (_stripes[i])
            {
                _dictionaries[i].Clear();
            }
        }
        
        while (_lockPool.TryDequeue(out var pooledLock))
        {
            pooledLock.Dispose();
        }

        _disposeCts.Dispose();
    }

    /// <summary>
    /// Asynchronously releases the resources used by this instance.
    /// </summary>
    /// <returns>A value task representing the asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
