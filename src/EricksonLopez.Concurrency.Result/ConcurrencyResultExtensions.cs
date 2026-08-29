// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Result;
using ResultInstance = EricksonLopez.Result.Result;

namespace EricksonLopez.Concurrency.Result;

/// <summary>
/// Provides extension methods converting concurrency outcomes, CAS results, and row counts into functional <see cref="ResultInstance"/> and <see cref="Result{TValue}"/>.
/// </summary>
public static class ConcurrencyResultExtensions
{
    /// <summary>
    /// Converts a <see cref="CasResult{TEntity}"/> into a functional <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the target entity.</typeparam>
    /// <param name="casResult">The Compare-And-Swap execution outcome to convert.</param>
    /// <returns>A successful result containing the mutated entity if the operation succeeded; otherwise, a conflict failure containing the structured error.</returns>
    public static Result<TEntity> ToResult<TEntity>(this CasResult<TEntity> casResult)
        where TEntity : class
    {
        if (casResult.IsSuccess && casResult.Entity is not null)
        {
            return Result<TEntity>.Success(casResult.Entity);
        }

        if (casResult.Conflict is not null)
        {
            Error error = ConcurrencyErrors.FromConflict(casResult.Conflict);
            return Result<TEntity>.Failure(error);
        }

        return Result<TEntity>.Failure(Error.Conflict(ConcurrencyErrors.ConcurrencyConflictCode, "Compare-and-swap operation failed without returning a state."));
    }

    /// <summary>
    /// Converts an optional <see cref="ConcurrencyConflict"/> into a non-generic <see cref="ResultInstance"/>.
    /// </summary>
    /// <param name="conflict">The conflict descriptor to convert, or <see langword="null"/> if the operation was successful.</param>
    /// <returns>A successful <see cref="ResultInstance"/> if <paramref name="conflict"/> is <see langword="null"/>; otherwise, a conflict failure containing the structured error.</returns>
    public static ResultInstance ToResult(this ConcurrencyConflict? conflict)
    {
        if (conflict is null)
        {
            return ResultInstance.Success();
        }

        Error error = ConcurrencyErrors.FromConflict(conflict);
        return ResultInstance.Failure(error);
    }

    /// <summary>
    /// Converts a <see cref="ConflictResolution{TEntity}"/> into a functional <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the target entity.</typeparam>
    /// <param name="resolution">The conflict resolution outcome to convert.</param>
    /// <returns>A successful result containing the resolved entity if reconciled; otherwise, a conflict failure.</returns>
    public static Result<TEntity> ToResult<TEntity>(this ConflictResolution<TEntity> resolution)
        where TEntity : class
    {
        if (resolution.IsResolved && resolution.ResolvedEntity is not null)
        {
            return Result<TEntity>.Success(resolution.ResolvedEntity);
        }

        return Result<TEntity>.Failure(Error.Conflict(ConcurrencyErrors.ConcurrencyConflictCode, resolution.Reason ?? "Conflict rejected by resolution policy."));
    }

    /// <summary>
    /// Evaluates the number of rows affected by an optimistic UPDATE command and returns a <see cref="ResultInstance"/>.
    /// </summary>
    /// <param name="rowsAffected">The number of rows modified by the database command.</param>
    /// <param name="entityId">The unique identifier of the target entity.</param>
    /// <param name="entityType">The type name of the target entity.</param>
    /// <param name="expectedVersion">The expected version constraint used in the query.</param>
    /// <returns>A successful <see cref="ResultInstance"/> if <paramref name="rowsAffected"/> is greater than zero; otherwise, a structured conflict failure.</returns>
    public static ResultInstance FromRowsAffected(
        int rowsAffected,
        string entityId,
        string entityType,
        ExpectedVersion expectedVersion)
    {
        if (rowsAffected > 0)
        {
            return ResultInstance.Success();
        }

        Error error = ConcurrencyErrors.VersionMismatch(entityId, entityType, expectedVersion);
        return ResultInstance.Failure(error);
    }

    /// <summary>
    /// Evaluates the number of rows affected by an optimistic UPDATE command and returns the entity in a <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the target entity.</typeparam>
    /// <param name="rowsAffected">The number of rows modified by the database command.</param>
    /// <param name="entity">The entity instance to return upon success.</param>
    /// <param name="entityId">The unique identifier of the target entity.</param>
    /// <param name="expectedVersion">The expected version constraint used in the query.</param>
    /// <returns>A successful result containing the entity if <paramref name="rowsAffected"/> is greater than zero; otherwise, a structured conflict failure.</returns>
    public static Result<TEntity> FromRowsAffected<TEntity>(
        int rowsAffected,
        TEntity entity,
        string entityId,
        ExpectedVersion expectedVersion)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (rowsAffected > 0)
        {
            return Result<TEntity>.Success(entity);
        }

        Error error = ConcurrencyErrors.VersionMismatch(entityId, typeof(TEntity).Name, expectedVersion);
        return Result<TEntity>.Failure(error);
    }
}
