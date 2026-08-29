// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using MySqlConnector;

namespace EricksonLopez.Concurrency.MariaDb;

/// <summary>
/// Classifies MariaDB error codes, deadlocks, lock wait timeouts, and unique constraint violations.
/// </summary>
public static class MariaDbConcurrencyErrorClassifier
{
    private const int DeadlockErrorNumber = 1213;         // ER_LOCK_DEADLOCK
    private const int LockWaitTimeoutErrorNumber = 1205;   // ER_LOCK_WAIT_TIMEOUT
    private const int DuplicateEntryErrorNumber = 1062;    // ER_DUP_ENTRY

    /// <summary>
    /// Determines whether the specified exception represents a MariaDB deadlock (Error 1213).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a deadlock; otherwise, <see langword="false"/>.</returns>
    public static bool IsDeadlock(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is MySqlException mySqlEx && mySqlEx.Number == DeadlockErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsDeadlock(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a lock wait timeout (Error 1205).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a lock timeout; otherwise, <see langword="false"/>.</returns>
    public static bool IsLockTimeout(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is MySqlException mySqlEx && mySqlEx.Number == LockWaitTimeoutErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsLockTimeout(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a duplicate key violation (Error 1062).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a duplicate entry; otherwise, <see langword="false"/>.</returns>
    public static bool IsUniqueViolation(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is MySqlException mySqlEx && mySqlEx.Number == DuplicateEntryErrorNumber)
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

        if (IsDeadlock(exception) || IsLockTimeout(exception))
        {
            return true;
        }

        if (exception is MySqlException mySqlEx)
        {
            return mySqlEx.Number switch
            {
                1042 => true, // ER_BAD_HOST_ERROR
                1053 => true, // ER_SERVER_SHUTDOWN
                1158 => true, // ER_NET_READ_ERROR_FROM_PIPE
                1159 => true, // ER_NET_READ_INTERRUPTED
                1160 => true, // ER_NET_ERROR_ON_WRITE
                1161 => true, // ER_NET_WRITE_INTERRUPTED
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
    /// Translates a <see cref="MySqlException"/> into a structured <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="exception">The MariaDB exception to translate.</param>
    /// <param name="entityId">The unique identifier of the entity involved.</param>
    /// <param name="entityType">The type name of the entity involved.</param>
    /// <param name="operation">The operation name being executed.</param>
    /// <returns>A populated <see cref="ConcurrencyConflict"/> if recognized as a concurrency error; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static ConcurrencyConflict? ToConcurrencyConflict(
        MySqlException exception,
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
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"MariaDB deadlock detected (Error {exception.Number}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "MariaDb",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (IsLockTimeout(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"MariaDB lock wait timeout exceeded (Error {exception.Number}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "MariaDb",
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
                message: $"MariaDB duplicate entry constraint violation (Error {exception.Number}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "MariaDb",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        return null;
    }
}
