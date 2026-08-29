// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.MariaDb;

/// <summary>
/// Provides extension methods for appending MariaDB locking clauses to query strings.
/// </summary>
public static class MariaDbLockExtensions
{
    /// <summary>
    /// Appends the appropriate MariaDB locking clause to the specified SELECT query.
    /// </summary>
    /// <param name="sqlQuery">The base SQL select query.</param>
    /// <param name="lockMode">The MariaDB lock mode to append.</param>
    /// <returns>The modified query with the locking clause attached.</returns>
    /// <exception cref="ArgumentException"><paramref name="sqlQuery"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lockMode"/> is not a valid <see cref="MariaDbLockMode"/></exception>
    public static string WithMariaDbLock(this string sqlQuery, MariaDbLockMode lockMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlQuery);

        string clause = lockMode switch
        {
            MariaDbLockMode.ForUpdate => "FOR UPDATE",
            MariaDbLockMode.ForUpdateNowait => "FOR UPDATE NOWAIT",
            MariaDbLockMode.ForUpdateSkipLocked => "FOR UPDATE SKIP LOCKED",
            MariaDbLockMode.LockInShareMode => "LOCK IN SHARE MODE",
            _ => throw new ArgumentOutOfRangeException(nameof(lockMode), lockMode, "Unsupported MariaDB lock mode.")
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
    public static string WithMariaDbLockWait(this string sqlQuery, int timeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlQuery);
        ArgumentOutOfRangeException.ThrowIfNegative(timeoutSeconds);

        string trimmed = sqlQuery.TrimEnd(';', ' ');
        return $"{trimmed} FOR UPDATE WAIT {timeoutSeconds};";
    }
}
