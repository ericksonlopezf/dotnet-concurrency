// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Controllers;

/// <summary>
/// Coordinates optimistic concurrency validations and in-memory Compare-And-Swap (CAS) state transitions.
/// </summary>
public sealed class ConcurrencyController : IConcurrencyController
{
    private readonly IConcurrencyChecker _checker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyController"/> class with the specified concurrency checker.
    /// </summary>
    /// <param name="checker">The concurrency checker used to evaluate version and token constraints, or <see langword="null"/> to use <see cref="OptimisticConcurrencyChecker.Instance"/>.</param>
    public ConcurrencyController(IConcurrencyChecker? checker = null)
    {
        _checker = checker ?? OptimisticConcurrencyChecker.Instance;
    }

    /// <inheritdoc />
    public ConcurrencyConflict? VerifyVersion<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId)
        where TEntity : class, IVersionedEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        string entityType = typeof(TEntity).Name;
        using Activity? activity = ConcurrencyDiagnostics.StartActivity("concurrency.verify_version", entityType, entityId);

        var actual = new ConcurrencyVersion(entity.Version);
        if (_checker.CheckVersion(expected, actual, entityId, entityType, out ConcurrencyConflict? conflict))
        {
            ConcurrencyDiagnostics.RecordSuccess(activity, entityType);
            return null;
        }

        ConcurrencyDiagnostics.RecordConflict(activity, nameof(ConcurrencyConflictType.VersionMismatch), entityType);
        return conflict;
    }

    /// <inheritdoc />
    public ConcurrencyConflict? VerifyToken<TEntity>(
        TEntity entity,
        IConcurrencyToken expected,
        string entityId)
        where TEntity : class, IConcurrencyAware
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(expected);

        string entityType = typeof(TEntity).Name;
        using Activity? activity = ConcurrencyDiagnostics.StartActivity("concurrency.verify_token", entityType, entityId);

        IConcurrencyToken actual = entity.ConcurrencyToken;
        if (_checker.CheckToken(expected, actual, entityId, entityType, out ConcurrencyConflict? conflict))
        {
            ConcurrencyDiagnostics.RecordSuccess(activity, entityType);
            return null;
        }

        ConcurrencyDiagnostics.RecordConflict(activity, nameof(ConcurrencyConflictType.TokenMismatch), entityType);
        return conflict;
    }

    /// <inheritdoc />
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

        string entityType = typeof(TEntity).Name;
        using Activity? activity = ConcurrencyDiagnostics.StartActivity("concurrency.execute_cas", entityType, entityId);

        var currentVersion = new ConcurrencyVersion(entity.Version);
        if (!_checker.CheckVersion(expected, currentVersion, entityId, entityType, out ConcurrencyConflict? conflict))
        {
            ConcurrencyDiagnostics.RecordConflict(activity, nameof(ConcurrencyConflictType.VersionMismatch), entityType);
            return CasResult.Conflicted<TEntity>(conflict);
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        TEntity mutated = await mutate(entity, cancellationToken).ConfigureAwait(false);
        double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        ConcurrencyDiagnostics.OperationDurationHistogram.Record(elapsedMs);

        ConcurrencyVersion nextVersion = currentVersion.Next();
        ConcurrencyDiagnostics.RecordSuccess(activity, entityType);

        return CasResult.Succeeded(mutated, nextVersion);
    }
}
