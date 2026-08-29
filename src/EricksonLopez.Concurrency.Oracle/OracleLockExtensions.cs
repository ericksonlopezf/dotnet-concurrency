// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Oracle;

/// <summary>
/// Provides extension methods for appending Oracle locking clauses to query strings.
/// </summary>
public static class OracleLockExtensions
{
    /// <summary>
    /// Appends the appropriate Oracle locking clause to the specified SELECT query.
    /// </summary>
    /// <param name="sqlQuery">The base SQL select query.</param>
    /// <param name="lockMode">The Oracle lock mode to append.</param>
    /// <returns>The modified query with the locking clause attached.</returns>
    /// <exception cref="ArgumentException"><paramref name="sqlQuery"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lockMode"/> is not a valid <see cref="OracleLockMode"/></exception>
    public static string WithOracleLock(this string sqlQuery, OracleLockMode lockMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlQuery);

        string clause = lockMode switch
        {
            OracleLockMode.ForUpdate => "FOR UPDATE",
            OracleLockMode.ForUpdateNowait => "FOR UPDATE NOWAIT",
            OracleLockMode.ForUpdateSkipLocked => "FOR UPDATE SKIP LOCKED",
            _ => throw new ArgumentOutOfRangeException(nameof(lockMode), lockMode, "Unsupported Oracle lock mode.")
        };

        string trimmed = sqlQuery.TrimEnd(';', ' ');
        return $"{trimmed} {clause};";
    }

    /// <summary>
    /// Appends a <c>FOR UPDATE WAIT [seconds]</c> clause to the specified SELECT query.
    /// </summary>
    /// <param name="sqlQuery">The base SQL select query.</param>
    /// <param name="timeoutSeconds">The lock wait timeout in seconds.</param>
    /// <returns>The modified query with the timed lock clause attached.</returns>
    /// <exception cref="ArgumentException"><paramref name="sqlQuery"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeoutSeconds"/> is negative</exception>
    public static string WithOracleLockWait(this string sqlQuery, int timeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlQuery);
        ArgumentOutOfRangeException.ThrowIfNegative(timeoutSeconds);

        string trimmed = sqlQuery.TrimEnd(';', ' ');
        return $"{trimmed} FOR UPDATE WAIT {timeoutSeconds};";
    }
}
