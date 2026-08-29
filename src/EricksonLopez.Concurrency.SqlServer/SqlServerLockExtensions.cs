// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.SqlServer;

/// <summary>
/// Provides extension methods for appending SQL Server table hints to SQL query strings.
/// </summary>
public static class SqlServerLockExtensions
{
    /// <summary>
    /// Appends the appropriate SQL Server table hint clause after table identifiers.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="lockMode">The SQL Server lock hint mode to apply.</param>
    /// <returns>The table name decorated with table hints.</returns>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lockMode"/> is not a valid <see cref="SqlServerLockMode"/></exception>
    public static string WithSqlServerTableHint(this string tableName, SqlServerLockMode lockMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        string hint = lockMode switch
        {
            SqlServerLockMode.UpdLockRowLock => "WITH (UPDLOCK, ROWLOCK)",
            SqlServerLockMode.UpdLockRowLockNowait => "WITH (UPDLOCK, ROWLOCK, NOWAIT)",
            SqlServerLockMode.UpdLockRowLockReadPast => "WITH (UPDLOCK, ROWLOCK, READPAST)",
            SqlServerLockMode.XLockRowLock => "WITH (XLOCK, ROWLOCK)",
            SqlServerLockMode.XLockRowLockNowait => "WITH (XLOCK, ROWLOCK, NOWAIT)",
            _ => throw new ArgumentOutOfRangeException(nameof(lockMode), lockMode, "Unsupported SQL Server lock mode.")
        };

        return $"{tableName} {hint}";
    }
}
