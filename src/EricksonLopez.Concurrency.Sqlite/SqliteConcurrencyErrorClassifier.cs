// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.Concurrency.Sqlite;

/// <summary>
/// Classifies SQLite error codes, database busy conditions, table lock collisions, and constraint violations.
/// </summary>
public static class SqliteConcurrencyErrorClassifier
{
    private const int SqliteBusy = 5;         // SQLITE_BUSY: The database file is locked
    private const int SqliteLocked = 6;       // SQLITE_LOCKED: A table in the database is locked
    private const int SqliteConstraint = 19;  // SQLITE_CONSTRAINT: Constraint violation (unique, etc.)

    /// <summary>
    /// Determines whether the specified exception represents a SQLite busy condition (Error 5).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the database is busy; otherwise, <see langword="false"/>.</returns>
    public static bool IsBusy(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqliteException sqlEx && sqlEx.SqliteErrorCode == SqliteBusy)
        {
            return true;
        }

        return exception.InnerException is not null && IsBusy(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a SQLite table locked condition (Error 6).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the table is locked; otherwise, <see langword="false"/>.</returns>
    public static bool IsLocked(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqliteException sqlEx && sqlEx.SqliteErrorCode == SqliteLocked)
        {
            return true;
        }

        return exception.InnerException is not null && IsLocked(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a SQLite constraint violation (Error 19).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if a constraint violation occurred; otherwise, <see langword="false"/>.</returns>
    public static bool IsConstraintViolation(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is SqliteException sqlEx && sqlEx.SqliteErrorCode == SqliteConstraint)
        {
            return true;
        }

        return exception.InnerException is not null && IsConstraintViolation(exception.InnerException);
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

        if (IsBusy(exception) || IsLocked(exception))
        {
            return true;
        }

        if (exception is TimeoutException)
        {
            return true;
        }

        return exception.InnerException is not null && IsTransient(exception.InnerException);
    }

    /// <summary>
    /// Translates a <see cref="SqliteException"/> into a structured <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="exception">The SQLite exception to translate.</param>
    /// <param name="entityId">The unique identifier of the entity involved.</param>
    /// <param name="entityType">The type name of the entity involved.</param>
    /// <param name="operation">The operation name being executed.</param>
    /// <returns>A populated <see cref="ConcurrencyConflict"/> if recognized as a concurrency error; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static ConcurrencyConflict? ToConcurrencyConflict(
        SqliteException exception,
        string entityId,
        string entityType,
        string? operation = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsBusy(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"SQLite database busy lock conflict (Error {exception.SqliteErrorCode}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Sqlite",
                    ["errorCode"] = exception.SqliteErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (IsLocked(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"SQLite table locked conflict (Error {exception.SqliteErrorCode}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Sqlite",
                    ["errorCode"] = exception.SqliteErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (IsConstraintViolation(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.Custom,
                classification: ConcurrencyConflictClassification.StaleState,
                operation: operation ?? "Update",
                message: $"SQLite constraint violation (Error {exception.SqliteErrorCode}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Sqlite",
                    ["errorCode"] = exception.SqliteErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        return null;
    }
}
