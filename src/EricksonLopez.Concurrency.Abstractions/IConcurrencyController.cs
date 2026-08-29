// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a core orchestrator contract for validating concurrency contracts and executing Compare-And-Swap (CAS) state mutations.
/// </summary>
public interface IConcurrencyController
{
    /// <summary>
    /// Evaluates optimistic version compatibility against an entity instance.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity implementing <see cref="IVersionedEntity"/>.</typeparam>
    /// <param name="entity">The entity instance to verify.</param>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <returns>A <see cref="ConcurrencyConflict"/> if a version conflict is detected; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/>.</exception>
    ConcurrencyConflict? VerifyVersion<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId)
        where TEntity : class, IVersionedEntity;

    /// <summary>
    /// Evaluates optimistic concurrency token compatibility against an entity instance.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity implementing <see cref="IConcurrencyAware"/>.</typeparam>
    /// <param name="entity">The entity instance to verify.</param>
    /// <param name="expected">The expected concurrency token constraint.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <returns>A <see cref="ConcurrencyConflict"/> if a token conflict is detected; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="expected"/> is <see langword="null"/>.</exception>
    ConcurrencyConflict? VerifyToken<TEntity>(
        TEntity entity,
        IConcurrencyToken expected,
        string entityId)
        where TEntity : class, IConcurrencyAware;

    /// <summary>
    /// Executes an in-memory optimistic Compare-And-Swap (CAS) state transition on a versioned entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity implementing <see cref="IVersionedEntity"/>.</typeparam>
    /// <param name="entity">The current entity instance before mutation.</param>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="mutate">The asynchronous mutation delegate that produces the modified entity.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="CasResult{TEntity}"/> outcome.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="mutate"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> has been cancelled.</exception>
    ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId,
        Func<TEntity, CancellationToken, ValueTask<TEntity>> mutate,
        CancellationToken cancellationToken = default)
        where TEntity : class, IVersionedEntity;
}
