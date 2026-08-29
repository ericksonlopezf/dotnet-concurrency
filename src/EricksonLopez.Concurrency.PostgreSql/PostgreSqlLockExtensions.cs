// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.PostgreSql;

/// <summary>
/// Provides extension methods for constructing PostgreSQL row-level locking SQL clauses.
/// </summary>
public static class PostgreSqlLockExtensions
{
    /// <summary>
    /// Converts a <see cref="PostgreSqlLockMode"/> to its corresponding SQL locking clause.
    /// </summary>
    /// <param name="mode">The PostgreSQL lock mode to convert.</param>
    /// <returns>The raw SQL locking clause string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a valid <see cref="PostgreSqlLockMode"/></exception>
    public static string ToSqlClause(this PostgreSqlLockMode mode) => mode switch
    {
        PostgreSqlLockMode.ForUpdate => "FOR UPDATE",
        PostgreSqlLockMode.ForUpdateNoWait => "FOR UPDATE NOWAIT",
        PostgreSqlLockMode.ForUpdateSkipLocked => "FOR UPDATE SKIP LOCKED",
        PostgreSqlLockMode.ForShare => "FOR SHARE",
        PostgreSqlLockMode.ForNoKeyUpdate => "FOR NO KEY UPDATE",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported PostgreSQL lock mode.")
    };

    /// <summary>
    /// Appends a PostgreSQL row-level locking clause to the specified SELECT statement.
    /// </summary>
    /// <param name="selectSql">The base SELECT statement.</param>
    /// <param name="mode">The lock mode to append.</param>
    /// <returns>The modified SQL query with the lock clause appended.</returns>
    /// <exception cref="ArgumentException"><paramref name="selectSql"/> is <see langword="null"/> or whitespace</exception>
    public static string WithLock(this string selectSql, PostgreSqlLockMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectSql);
        string trimmed = selectSql.TrimEnd(';', ' ');
        return $"{trimmed} {mode.ToSqlClause()};";
    }
}
