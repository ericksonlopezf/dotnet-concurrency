// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace EricksonLopez.Concurrency.Oracle;

/// <summary>
/// Classifies Oracle error codes, deadlocks, serialization conflicts, and resource busy conditions.
/// </summary>
public static class OracleConcurrencyErrorClassifier
{
    private const int DeadlockErrorNumber = 60;              // ORA-00060: deadlock detected
    private const int ResourceBusyErrorNumber = 54;          // ORA-00054: resource busy and acquire with NOWAIT specified
    private const int SerializationFailureErrorNumber = 8177;// ORA-08177: can't serialize access for this transaction
    private const int UniqueViolationErrorNumber = 1;        // ORA-00001: unique constraint violated

    /// <summary>
    /// Determines whether the specified exception represents an Oracle deadlock (ORA-00060).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a deadlock; otherwise, <see langword="false"/>.</returns>
    public static bool IsDeadlock(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is OracleException oraEx && oraEx.Number == DeadlockErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsDeadlock(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents an Oracle resource busy condition (ORA-00054).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the resource is busy; otherwise, <see langword="false"/>.</returns>
    public static bool IsResourceBusy(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is OracleException oraEx && oraEx.Number == ResourceBusyErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsResourceBusy(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents an Oracle serialization failure (ORA-08177).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a serialization failure; otherwise, <see langword="false"/>.</returns>
    public static bool IsSerializationFailure(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is OracleException oraEx && oraEx.Number == SerializationFailureErrorNumber)
        {
            return true;
        }

        return exception.InnerException is not null && IsSerializationFailure(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents an Oracle unique constraint violation (ORA-00001).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the error represents a unique constraint violation; otherwise, <see langword="false"/>.</returns>
    public static bool IsUniqueViolation(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is OracleException oraEx && oraEx.Number == UniqueViolationErrorNumber)
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

        if (IsDeadlock(exception) || IsResourceBusy(exception) || IsSerializationFailure(exception))
        {
            return true;
        }

        if (exception is OracleException oraEx)
        {
            return oraEx.Number switch
            {
                3113 => true,  // ORA-03113: end-of-file on communication channel
                3114 => true,  // ORA-03114: not connected to ORACLE
                12170 => true, // ORA-12170: TNS:Connect timeout occurred
                12541 => true, // ORA-12541: TNS:no listener
                12543 => true, // ORA-12543: TNS:destination host unreachable
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
    /// Translates an <see cref="OracleException"/> into a structured <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="exception">The Oracle exception to translate.</param>
    /// <param name="entityId">The unique identifier of the entity involved.</param>
    /// <param name="entityType">The type name of the entity involved.</param>
    /// <param name="operation">The operation name being executed.</param>
    /// <returns>A populated <see cref="ConcurrencyConflict"/> if recognized as a concurrency error; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static ConcurrencyConflict? ToConcurrencyConflict(
        OracleException exception,
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
                message: $"Oracle deadlock detected (ORA-{exception.Number:D5}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Oracle",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (IsResourceBusy(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation ?? "Update",
                message: $"Oracle resource busy condition (ORA-{exception.Number:D5}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Oracle",
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
                message: $"Oracle serialization conflict (ORA-{exception.Number:D5}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Oracle",
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
                message: $"Oracle unique constraint violation (ORA-{exception.Number:D5}): {exception.Message}",
                metadata: new Dictionary<string, string>
                {
                    ["provider"] = "Oracle",
                    ["errorNumber"] = exception.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        return null;
    }
}
