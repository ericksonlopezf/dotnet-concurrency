// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using Microsoft.Data.SqlClient;

namespace EricksonLopez.Concurrency.SqlServer;

/// <summary>
/// Classifies Microsoft SQL Server error codes, deadlocks, and snapshot isolation serialization conflicts.
/// </summary>
public static class SqlServerErrorClassifier
{
    private const int DeadlockErrorNumber = 1205;
    private const int LockTimeoutErrorNumber = 1222;
    private const int SnapshotConflictErrorNumber = 3960;
    private const int UpdateConflictErrorNumber = 3961;
    private const int UniqueConstraintViolation = 2601;
    private const int PrimaryKeyViolation = 2627;
    private const int TimeoutExpired = -2;

    /// <summary>
    /// Determines whether the specified exception represents a SQL Server deadlock (Error 1205).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a deadlock; otherwise, <see langword="false"/>.</returns>
    public static bool IsDeadlock(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqlException sqlEx && sqlEx.Number == DeadlockErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsDeadlock(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a snapshot isolation update conflict (Error 3960 or 3961).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a serialization conflict; otherwise, <see langword="false"/>.</returns>
    public static bool IsSerializationFailure(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqlException sqlEx &&
            (sqlEx.Number == SnapshotConflictErrorNumber || sqlEx.Number == UpdateConflictErrorNumber))
        {
            return true;
        }

        return exception.InnerException is not null && IsSerializationFailure(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a SQL Server lock request timeout (Error 1222).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the lock request timed out; otherwise, <see langword="false"/>.</returns>
    public static bool IsLockTimeout(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqlException sqlEx && sqlEx.Number == LockTimeoutErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsLockTimeout(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a unique constraint or primary key violation (Error 2601 or 2627).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a unique violation; otherwise, <see langword="false"/>.</returns>
    public static bool IsUniqueViolation(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqlException sqlEx &&
            (sqlEx.Number == UniqueConstraintViolation || sqlEx.Number == PrimaryKeyViolation))
        {
            return true;
        }

        return exception.InnerException is not null && IsUniqueViolation(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a transient failure suitable for retry.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error is transient; otherwise, <see langword="false"/>.</returns>
    public static bool IsTransient(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (IsDeadlock(exception) || IsSerializationFailure(exception) || IsLockTimeout(exception))
        {
            return true;
        }

        if (exception is SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                TimeoutExpired => true,
                4060 => true,   // Cannot open database requested by the login
                40197 => true,  // Error processing the request
                40501 => true,  // Server is busy
                40613 => true,  // Database is not currently available
                49918 => true,  // Cannot process request. Not enough resources
                49919 => true,  // Cannot process create or update request. Too many operations
                49920 => true,  // Cannot process request. Too many operations
                _ => false
            };
        }

        if (exception is TimeoutException)
        {
            return true;
        }

        return exception.InnerException is not null && IsTransient(exception.InnerException);
    }

    /// <summary>
    /// Translates a <see cref="SqlException"/> into a structured <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="exception">The SQL Server exception to translate.</param>
    /// <param name="entityId">The unique identifier of the entity involved.</param>
    /// <param name="entityType">The type name of the entity involved.</param>
    /// <param name="operation">The operation name being executed.</param>
    /// <returns>A populated <see cref="ConcurrencyConflict"/> if recognized as a concurrency error; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static ConcurrencyConflict? ToConcurrencyConflict(
        SqlException exception,
        string entityId,
        string entityType,
        string? operation = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsDeadlock(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.Deadlock,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"SQL Server deadlock detected (Error {exception.Number}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "SqlServer",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (IsSerializationFailure(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"SQL Server snapshot isolation conflict (Error {exception.Number}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "SqlServer",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (IsUniqueViolation(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.Custom,
                classification: ConcurrencyConflictClassification.StaleState,
                operation: operation ?? "Update",
                message: $"SQL Server unique constraint violation (Error {exception.Number}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "SqlServer",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        return null;
    }
}
