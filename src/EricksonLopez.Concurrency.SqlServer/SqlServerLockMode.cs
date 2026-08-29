// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.SqlServer;

/// <summary>
/// Specifies Microsoft SQL Server table hints for pessimistic row locking and synchronization.
/// </summary>
public enum SqlServerLockMode
{
    /// <summary>
    /// Update lock on row level (<c>WITH (UPDLOCK, ROWLOCK)</c>).
    /// </summary>
    UpdLockRowLock,

    /// <summary>
    /// Update lock on row level with immediate failure if locked (<c>WITH (UPDLOCK, ROWLOCK, NOWAIT)</c>).
    /// </summary>
    UpdLockRowLockNowait,

    /// <summary>
    /// Update lock skipping rows locked by other transactions (<c>WITH (UPDLOCK, ROWLOCK, READPAST)</c>).
    /// </summary>
    UpdLockRowLockReadPast,

    /// <summary>
    /// Exclusive lock on row level (<c>WITH (XLOCK, ROWLOCK)</c>).
    /// </summary>
    XLockRowLock,

    /// <summary>
    /// Exclusive lock on row level with immediate failure if locked (<c>WITH (XLOCK, ROWLOCK, NOWAIT)</c>).
    /// </summary>
    XLockRowLockNowait
}
