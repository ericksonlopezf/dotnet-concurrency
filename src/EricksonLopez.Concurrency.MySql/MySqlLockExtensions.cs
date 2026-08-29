// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.MySql;

/// <summary>
/// Provides extension methods for appending MySQL locking clauses to query strings.
/// </summary>
public static class MySqlLockExtensions
{
    /// <summary>
    /// Appends the appropriate MySQL locking clause to the specified SELECT query.
    /// </summary>
    /// <param name="sqlQuery">The base SQL select query.</param>
    /// <param name="lockMode">The MySQL lock mode to append.</param>
    /// <returns>The modified query with the locking clause attached.</returns>
    /// <exception cref="ArgumentException"><paramref name="sqlQuery"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lockMode"/> is not a valid <see cref="MySqlLockMode"/></exception>
    public static string WithMySqlLock(this string sqlQuery, MySqlLockMode lockMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlQuery);

        string clause = lockMode switch
        {
            MySqlLockMode.ForUpdate => "FOR UPDATE",
            MySqlLockMode.ForUpdateNowait => "FOR UPDATE NOWAIT",
            MySqlLockMode.ForUpdateSkipLocked => "FOR UPDATE SKIP LOCKED",
            MySqlLockMode.ForShare => "FOR SHARE",
            _ => throw new ArgumentOutOfRangeException(nameof(lockMode), lockMode, "Unsupported MySQL lock mode.")
        };

        string trimmed = sqlQuery.TrimEnd(';', ' ');
        return $"{trimmed} {clause};";
    }
}
