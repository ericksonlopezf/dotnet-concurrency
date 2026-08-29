// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Dapper;

/// <summary>
/// Provides extension methods for <see cref="IDbConnection"/> to execute optimistic concurrency commands with automated conflict detection.
/// </summary>
public static class ConcurrencyDapperExtensions
{
    /// <summary>
    /// Executes a parameterized SQL command and evaluates whether the affected row count satisfies the optimistic version condition.
    /// </summary>
    /// <param name="connection">The database connection to execute the command against.</param>
    /// <param name="sql">The SQL statement containing optimistic WHERE conditions (e.g. <c>WHERE id = @Id AND version = @ExpectedVersion</c>).</param>
    /// <param name="param">The command parameters to pass to the query.</param>
    /// <param name="expectedVersion">The expected version constraint.</param>
    /// <param name="entityId">The unique identifier of the entity being updated.</param>
    /// <param name="entityType">The type name of the entity being updated.</param>
    /// <param name="transaction">The active database transaction, if any.</param>
    /// <param name="commandTimeout">The command timeout in seconds, or <see langword="null"/> to use the default timeout.</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="ConcurrencyConflict"/> if no rows were modified; otherwise, <see langword="null"/> indicating success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is <see langword="null"/> or whitespace</exception>
    public static async Task<ConcurrencyConflict?> ExecuteOptimisticAsync(
        this IDbConnection connection,
        string sql,
        object? param,
        ExpectedVersion expectedVersion,
        string entityId,
        string entityType,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            commandType: commandType,
            flags: CommandFlags.Buffered,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);

        if (rowsAffected > 0)
        {
            ConcurrencyDiagnostics.SuccessesCounter.Add(1, new KeyValuePair<string, object?>("concurrency.entity_type", entityType));
            return null;
        }

        ConcurrencyConflict conflict = ConcurrencyConflict.VersionMismatch(
            entityId: entityId,
            entityType: entityType,
            expected: expectedVersion,
            actual: null,
            operation: "ExecuteOptimisticUpdate");

        ConcurrencyDiagnostics.RecordConflict(null, nameof(ConcurrencyConflictType.VersionMismatch), entityType);
        return conflict;
    }

    /// <summary>
    /// Executes a parameterized SQL command and evaluates whether the affected row count satisfies the optimistic token condition.
    /// </summary>
    /// <param name="connection">The database connection to execute the command against.</param>
    /// <param name="sql">The SQL statement containing optimistic WHERE conditions for concurrency tokens.</param>
    /// <param name="param">The command parameters to pass to the query.</param>
    /// <param name="expectedToken">The expected concurrency token constraint.</param>
    /// <param name="entityId">The unique identifier of the entity being updated.</param>
    /// <param name="entityType">The type name of the entity being updated.</param>
    /// <param name="transaction">The active database transaction, if any.</param>
    /// <param name="commandTimeout">The command timeout in seconds, or <see langword="null"/> to use the default timeout.</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="ConcurrencyConflict"/> if no rows were modified; otherwise, <see langword="null"/> indicating success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="expectedToken"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is <see langword="null"/> or whitespace</exception>
    public static async Task<ConcurrencyConflict?> ExecuteOptimisticTokenAsync(
        this IDbConnection connection,
        string sql,
        object? param,
        IConcurrencyToken expectedToken,
        string entityId,
        string entityType,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(expectedToken);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            commandType: commandType,
            flags: CommandFlags.Buffered,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);

        if (rowsAffected > 0)
        {
            ConcurrencyDiagnostics.SuccessesCounter.Add(1, new KeyValuePair<string, object?>("concurrency.entity_type", entityType));
            return null;
        }

        ConcurrencyConflict conflict = ConcurrencyConflict.TokenMismatch(
            entityId: entityId,
            entityType: entityType,
            expected: expectedToken,
            actual: null,
            operation: "ExecuteOptimisticTokenUpdate");

        ConcurrencyDiagnostics.RecordConflict(null, nameof(ConcurrencyConflictType.TokenMismatch), entityType);
        return conflict;
    }
}
