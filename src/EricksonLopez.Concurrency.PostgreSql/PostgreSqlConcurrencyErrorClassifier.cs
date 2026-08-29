// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using Npgsql;

namespace EricksonLopez.Concurrency.PostgreSql;

/// <summary>
/// Classifies PostgreSQL-specific error codes and exceptions into structured concurrency conflict models and resilience categories.
/// </summary>
public static class PostgreSqlConcurrencyErrorClassifier
{
    /// <summary>
    /// Specifies PostgreSQL SQLSTATE 40001 (serialization_failure under SERIALIZABLE or REPEATABLE READ isolation).
    /// </summary>
    public const string SerializationFailureSqlState = "40001";

    /// <summary>
    /// Specifies PostgreSQL SQLSTATE 40P01 (deadlock_detected between concurrent database transactions).
    /// </summary>
    public const string DeadlockDetectedSqlState = "40P01";

    /// <summary>
    /// Specifies PostgreSQL SQLSTATE 55P03 (lock_not_available when lock acquisition cannot be fulfilled immediately).
    /// </summary>
    public const string LockNotAvailableSqlState = "55P03";

    /// <summary>
    /// Specifies PostgreSQL SQLSTATE 23505 (unique_violation due to primary key or unique index collision).
    /// </summary>
    public const string UniqueViolationSqlState = "23505";

    /// <summary>
    /// Specifies PostgreSQL SQLSTATE 25P02 (in_failed_sql_transaction).
    /// </summary>
    public const string InFailedSqlTransactionSqlState = "25P02";

    /// <summary>
    /// Determines whether the specified exception represents a PostgreSQL transaction serialization conflict (SQLSTATE 40001).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception represents a serialization failure; otherwise, <see langword="false"/>.</returns>
    public static bool IsSerializationFailure(Exception? exception)
    {
        if (exception is PostgresException pgEx && pgEx.SqlState == SerializationFailureSqlState)
        {
            return true;
        }

        return exception?.InnerException is not null && IsSerializationFailure(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a detected PostgreSQL deadlock (SQLSTATE 40P01).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception represents a deadlock; otherwise, <see langword="false"/>.</returns>
    public static bool IsDeadlock(Exception? exception)
    {
        if (exception is PostgresException pgEx && pgEx.SqlState == DeadlockDetectedSqlState)
        {
            return true;
        }

        return exception?.InnerException is not null && IsDeadlock(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a PostgreSQL lock acquisition failure (SQLSTATE 55P03).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception represents an unavailable lock; otherwise, <see langword="false"/>.</returns>
    public static bool IsLockNotAvailable(Exception? exception)
    {
        if (exception is PostgresException pgEx && pgEx.SqlState == LockNotAvailableSqlState)
        {
            return true;
        }

        return exception?.InnerException is not null && IsLockNotAvailable(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a PostgreSQL unique constraint violation (SQLSTATE 23505).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception represents a unique constraint violation; otherwise, <see langword="false"/>.</returns>
    public static bool IsUniqueViolation(Exception? exception)
    {
        if (exception is PostgresException pgEx && pgEx.SqlState == UniqueViolationSqlState)
        {
            return true;
        }

        return exception?.InnerException is not null && IsUniqueViolation(exception.InnerException);
    }

    /// <summary>
    /// Determines whether the specified exception represents a transient PostgreSQL failure suitable for transaction retry.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception is transient; otherwise, <see langword="false"/>.</returns>
    public static bool IsTransient(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is PostgresException pgEx)
        {
            return pgEx.SqlState switch
            {
                SerializationFailureSqlState => true,
                DeadlockDetectedSqlState => true,
                "08006" => true, // Connection failure
                "57P01" => true, // Admin shutdown
                "57P02" => true, // Crash shutdown
                "57P03" => true, // Cannot connect now
                _ => false
            };
        }

        if (exception is NpgsqlException npgEx && npgEx.IsTransient)
        {
            return true;
        }

        return exception.InnerException is not null && IsTransient(exception.InnerException);
    }

    /// <summary>
    /// Translates recognized PostgreSQL concurrency exceptions into a structured <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="exception">The exception caught during database operations.</param>
    /// <param name="entityId">The unique identifier of the target entity.</param>
    /// <param name="entityType">The type name of the target entity.</param>
    /// <param name="operation">The name of the operation being executed.</param>
    /// <returns>A populated <see cref="ConcurrencyConflict"/> if recognized as a concurrency failure; otherwise, <see langword="null"/>.</returns>
    public static ConcurrencyConflict? ToConcurrencyConflict(
        Exception exception,
        string entityId,
        string entityType,
        string operation = "PostgreSqlOperation")
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsSerializationFailure(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.SerializationFailure,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation,
                message: $"PostgreSQL transaction serialization conflict (SQLSTATE 40001) on '{entityType}' with ID '{entityId}'.",
                metadata: new Dictionary<string, string> { ["sqlState"] = SerializationFailureSqlState });
        }

        if (IsDeadlock(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.Deadlock,
                classification: ConcurrencyConflictClassification.Transient,
                operation: operation,
                message: $"PostgreSQL deadlock detected (SQLSTATE 40P01) on '{entityType}' with ID '{entityId}'.",
                metadata: new Dictionary<string, string> { ["sqlState"] = DeadlockDetectedSqlState });
        }

        if (IsLockNotAvailable(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.LockUnavailable,
                classification: ConcurrencyConflictClassification.NonRetryable,
                operation: operation,
                message: $"PostgreSQL row lock unavailable (SQLSTATE 55P03) on '{entityType}' with ID '{entityId}'.",
                metadata: new Dictionary<string, string> { ["sqlState"] = LockNotAvailableSqlState });
        }

        if (IsUniqueViolation(exception))
        {
            return new ConcurrencyConflict(
                entityId: entityId,
                entityType: entityType,
                conflictType: ConcurrencyConflictType.AlreadyExists,
                classification: ConcurrencyConflictClassification.NonRetryable,
                operation: operation,
                message: $"PostgreSQL unique constraint violation (SQLSTATE 23505) on '{entityType}' with ID '{entityId}'.",
                metadata: new Dictionary<string, string> { ["sqlState"] = UniqueViolationSqlState });
        }

        return null;
    }
}
