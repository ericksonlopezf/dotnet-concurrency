// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Testing;

/// <summary>
/// Provides an in-memory, mock-free test double for <see cref="IConcurrencyController"/> with full call tracking and configurable outcomes.
/// </summary>
public sealed class FakeConcurrencyController : IConcurrencyController
{
    private readonly ConcurrentQueue<ConcurrencyConflict> _conflictQueue = new();
    private readonly ConcurrentQueue<long?> _successVersionQueue = new();
    private readonly List<VerifyVersionInvocation> _verifyVersionCalls = [];
    private readonly List<VerifyTokenInvocation> _verifyTokenCalls = [];
    private readonly List<ExecuteCasInvocation> _executeCasCalls = [];
    private readonly object _lock = new();

    private Func<object, ExpectedVersion, string, ConcurrencyConflict?>? _verifyVersionHandler;
    private Func<object, IConcurrencyToken, string, ConcurrencyConflict?>? _verifyTokenHandler;
    private ConcurrencyConflict? _fixedConflict;
    private long? _fixedSuccessVersion;

    /// <summary>
    /// Gets the list of recorded <see cref="IConcurrencyController.VerifyVersion{TEntity}"/> invocations.
    /// </summary>
    public IReadOnlyList<VerifyVersionInvocation> VerifyVersionInvocations
    {
        get
        {
            lock (_lock)
            {
                return _verifyVersionCalls.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the list of recorded <see cref="IConcurrencyController.VerifyToken{TEntity}"/> invocations.
    /// </summary>
    public IReadOnlyList<VerifyTokenInvocation> VerifyTokenInvocations
    {
        get
        {
            lock (_lock)
            {
                return _verifyTokenCalls.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the list of recorded <see cref="IConcurrencyController.ExecuteCasAsync{TEntity}"/> invocations.
    /// </summary>
    public IReadOnlyList<ExecuteCasInvocation> ExecuteCasInvocations
    {
        get
        {
            lock (_lock)
            {
                return _executeCasCalls.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the total number of invocations across all methods.
    /// </summary>
    public int TotalInvocations
    {
        get
        {
            lock (_lock)
            {
                return _verifyVersionCalls.Count + _verifyTokenCalls.Count + _executeCasCalls.Count;
            }
        }
    }

    /// <summary>
    /// Configures the fake controller to produce a successful outcome on all subsequent operations.
    /// </summary>
    /// <param name="nextVersion">The version value to return on CAS operations, or <see langword="null"/> to increment automatically.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    public FakeConcurrencyController WithSuccess(long? nextVersion = null)
    {
        lock (_lock)
        {
            _fixedConflict = null;
            _fixedSuccessVersion = nextVersion;
        }

        return this;
    }

    /// <summary>
    /// Configures the fake controller to enqueue a specific successful version for the next operation.
    /// </summary>
    /// <param name="nextVersion">The specific version value to return on the next CAS operation.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    public FakeConcurrencyController WithSuccessOnNextWrite(long? nextVersion = null)
    {
        _successVersionQueue.Enqueue(nextVersion);
        return this;
    }

    /// <summary>
    /// Configures the fake controller to always return the specified conflict on all subsequent operations.
    /// </summary>
    /// <param name="conflict">The conflict descriptor to return.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conflict"/> is <see langword="null"/></exception>
    public FakeConcurrencyController WithConflict(ConcurrencyConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        lock (_lock)
        {
            _fixedConflict = conflict;
        }

        return this;
    }

    /// <summary>
    /// Configures the fake controller to always return a synthesized conflict on all subsequent operations.
    /// </summary>
    /// <param name="type">The conflict category.</param>
    /// <param name="entityId">The optional entity identifier.</param>
    /// <param name="entityType">The optional entity type name.</param>
    /// <param name="classification">The conflict classification.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    public FakeConcurrencyController WithConflict(
        ConcurrencyConflictType type,
        string? entityId = null,
        string? entityType = null,
        ConcurrencyConflictClassification classification = ConcurrencyConflictClassification.Transient)
    {
        var conflict = new ConcurrencyConflict(
            entityId ?? "test-entity-id",
            entityType ?? "TestEntity",
            type,
            classification,
            "FakeOperation",
            $"Simulated {type} conflict.");

        return WithConflict(conflict);
    }

    /// <summary>
    /// Configures the fake controller to return the specified conflict on the next operation only.
    /// </summary>
    /// <param name="conflict">The conflict descriptor to return.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conflict"/> is <see langword="null"/></exception>
    public FakeConcurrencyController WithConflictOnNextWrite(ConcurrencyConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        _conflictQueue.Enqueue(conflict);
        return this;
    }

    /// <summary>
    /// Configures the fake controller to return a synthesized conflict on the next operation only.
    /// </summary>
    /// <param name="type">The conflict category.</param>
    /// <param name="entityId">The optional entity identifier.</param>
    /// <param name="entityType">The optional entity type name.</param>
    /// <param name="classification">The conflict classification.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    public FakeConcurrencyController WithConflictOnNextWrite(
        ConcurrencyConflictType type,
        string? entityId = null,
        string? entityType = null,
        ConcurrencyConflictClassification classification = ConcurrencyConflictClassification.Transient)
    {
        var conflict = new ConcurrencyConflict(
            entityId ?? "test-entity-id",
            entityType ?? "TestEntity",
            type,
            classification,
            "FakeOperation",
            $"Simulated {type} conflict.");

        return WithConflictOnNextWrite(conflict);
    }

    /// <summary>
    /// Configures a custom evaluation delegate for <see cref="VerifyVersion{TEntity}"/>.
    /// </summary>
    /// <param name="handler">The custom delegate evaluating version calls.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    public FakeConcurrencyController WhenVerifyVersion(Func<object, ExpectedVersion, string, ConcurrencyConflict?> handler)
    {
        lock (_lock)
        {
            _verifyVersionHandler = handler;
        }

        return this;
    }

    /// <summary>
    /// Configures a custom evaluation delegate for <see cref="VerifyToken{TEntity}"/>.
    /// </summary>
    /// <param name="handler">The custom delegate evaluating token calls.</param>
    /// <returns>This <see cref="FakeConcurrencyController"/> instance for fluent chaining.</returns>
    public FakeConcurrencyController WhenVerifyToken(Func<object, IConcurrencyToken, string, ConcurrencyConflict?> handler)
    {
        lock (_lock)
        {
            _verifyTokenHandler = handler;
        }

        return this;
    }

    /// <summary>
    /// Clears all recorded invocations and reset configured behaviors to default.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _verifyVersionCalls.Clear();
            _verifyTokenCalls.Clear();
            _executeCasCalls.Clear();
            _fixedConflict = null;
            _fixedSuccessVersion = null;
            _verifyVersionHandler = null;
            _verifyTokenHandler = null;
        }

        while (_conflictQueue.TryDequeue(out _)) { }
        while (_successVersionQueue.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/></exception>
    public ConcurrencyConflict? VerifyVersion<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId)
        where TEntity : class, IVersionedEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_lock)
        {
            _verifyVersionCalls.Add(new VerifyVersionInvocation(entity, expected, entityId, DateTimeOffset.UtcNow));

            if (_verifyVersionHandler is not null)
            {
                return _verifyVersionHandler(entity, expected, entityId);
            }
        }

        if (_conflictQueue.TryDequeue(out ConcurrencyConflict? queuedConflict))
        {
            return queuedConflict;
        }

        lock (_lock)
        {
            return _fixedConflict;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="expected"/> is <see langword="null"/></exception>
    public ConcurrencyConflict? VerifyToken<TEntity>(
        TEntity entity,
        IConcurrencyToken expected,
        string entityId)
        where TEntity : class, IConcurrencyAware
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(expected);

        lock (_lock)
        {
            _verifyTokenCalls.Add(new VerifyTokenInvocation(entity, expected, entityId, DateTimeOffset.UtcNow));

            if (_verifyTokenHandler is not null)
            {
                return _verifyTokenHandler(entity, expected, entityId);
            }
        }

        if (_conflictQueue.TryDequeue(out ConcurrencyConflict? queuedConflict))
        {
            return queuedConflict;
        }

        lock (_lock)
        {
            return _fixedConflict;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="mutate"/> is <see langword="null"/></exception>
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

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _executeCasCalls.Add(new ExecuteCasInvocation(entity, expected, entityId, DateTimeOffset.UtcNow));
        }

        if (_conflictQueue.TryDequeue(out ConcurrencyConflict? queuedConflict))
        {
            return CasResult.Conflicted<TEntity>(queuedConflict);
        }

        lock (_lock)
        {
            if (_fixedConflict is not null)
            {
                return CasResult.Conflicted<TEntity>(_fixedConflict);
            }
        }

        TEntity mutated = await mutate(entity, cancellationToken).ConfigureAwait(false);

        ConcurrencyVersion nextVersion;
        if (_successVersionQueue.TryDequeue(out long? queuedVersion) && queuedVersion.HasValue)
        {
            nextVersion = new ConcurrencyVersion(queuedVersion.Value);
        }
        else
        {
            lock (_lock)
            {
                if (_fixedSuccessVersion.HasValue)
                {
                    nextVersion = new ConcurrencyVersion(_fixedSuccessVersion.Value);
                }
                else
                {
                    nextVersion = new ConcurrencyVersion(entity.Version).Next();
                }
            }
        }

        return CasResult.Succeeded(mutated, nextVersion);
    }
}
