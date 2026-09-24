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
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="ObjectDisposedException">The controller instance has been disposed</exception>
    /// <exception cref="ConcurrencyException">A version conflict was detected and <c>ConcurrencyOptions.ThrowOnUnresolvedConflict</c> is <see langword="true"/></exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="expected"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="ObjectDisposedException">The controller instance has been disposed</exception>
    /// <exception cref="ConcurrencyException">A token conflict was detected and <c>ConcurrencyOptions.ThrowOnUnresolvedConflict</c> is <see langword="true"/></exception>
    ConcurrencyConflict? VerifyToken<TEntity>(
        TEntity entity,
        IConcurrencyToken expected,
        string entityId)
        where TEntity : class, IConcurrencyAware;

    /// <summary>
    /// Executes an in-memory optimistic Compare-And-Swap (CAS) state transition on a versioned entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enforces per-entity mutual exclusion in memory. Invocations on the same <paramref name="entityId"/>
    /// are strictly non-reentrant; attempting nested or recursive calls to <c>ExecuteCasAsync</c> for the same entity identifier
    /// within the same asynchronous flow will throw an <see cref="InvalidOperationException"/> to prevent deadlocks.
    /// </para>
    /// <para>
    /// If the target entity implements <see cref="IMutableVersionedEntity"/>, its <see cref="IMutableVersionedEntity.Version"/>
    /// is automatically advanced by the controller upon success. If the entity does not implement <see cref="IMutableVersionedEntity"/>,
    /// the <paramref name="mutate"/> delegate must produce an entity whose version matches the incremented version.
    /// </para>
    /// <para>
    /// <strong>Warning (Data Corruption Risk):</strong> The <c>ExecuteCasAsync</c> method does not provide state rollback. If the <paramref name="mutate"/> delegate
    /// modifies the entity and subsequently throws an exception, the entity remains in a partially modified state.
    /// Always pass a deep clone, use C# 9 <c>record</c> copy-semantics (with-expressions), or discard the entity reference on failure.
    /// </para>
    /// </remarks>
    /// <typeparam name="TEntity">The type of the entity implementing <see cref="IVersionedEntity"/>.</typeparam>
    /// <param name="entity">The current entity instance before mutation.</param>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="mutate">The asynchronous mutation delegate that produces the modified entity.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="CasResult{TEntity}"/> outcome.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="mutate"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="InvalidOperationException">A recursive/reentrant call was attempted on the same <paramref name="entityId"/>, or an immutable entity did not advance its version</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> has been cancelled</exception>
    /// <exception cref="TimeoutException">The in-memory lock could not be acquired within the configured timeout, potentially due to a deadlock or a long-running operation</exception>
    ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId,
        Func<TEntity, CancellationToken, ValueTask<TEntity>> mutate,
        CancellationToken cancellationToken = default)
        where TEntity : class, IVersionedEntity;

    /// <summary>
    /// Executes an in-memory optimistic Compare-And-Swap (CAS) state transition on a versioned entity using a synchronous mutation delegate.
    /// </summary>
    /// <remarks>
    /// This method is a convenience overload that wraps the synchronous <paramref name="mutate"/> delegate in a <see cref="ValueTask{TResult}"/>.
    /// It provides the exact same guarantees and semantics as the asynchronous counterpart.
    /// Use immutable types (e.g., records) in the delegate to prevent state corruption upon exceptions.
    /// </remarks>
    /// <typeparam name="TEntity">The type of the entity implementing <see cref="IVersionedEntity"/>.</typeparam>
    /// <param name="entity">The current entity instance before mutation.</param>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="mutate">The synchronous mutation delegate that produces the modified entity.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="CasResult{TEntity}"/> outcome.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> or <paramref name="mutate"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="entityId"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="InvalidOperationException">A recursive/reentrant call was attempted on the same <paramref name="entityId"/>, or an immutable entity did not advance its version</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> has been cancelled</exception>
    /// <exception cref="TimeoutException">The in-memory lock could not be acquired within the configured timeout, potentially due to a deadlock or a long-running operation</exception>
    ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
        TEntity entity,
        ExpectedVersion expected,
        string entityId,
        Func<TEntity, CancellationToken, TEntity> mutate,
        CancellationToken cancellationToken = default)
        where TEntity : class, IVersionedEntity
    {
        return ExecuteCasAsync(
            entity,
            expected,
            entityId,
            (e, ct) => ValueTask.FromResult(mutate(e, ct)),
            cancellationToken);
    }
}
